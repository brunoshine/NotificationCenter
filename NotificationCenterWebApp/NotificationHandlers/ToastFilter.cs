using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NotificationCenterWebApp.NotificationHandlers
{
    /// <summary>
    /// Mostly adapted from Glimpses HTTPModule @https://github.com/Glimpse/Glimpse/blob/master/source/Glimpse.AspNet/HttpModule.cs
    /// </summary>
    public class ToastFilter : Stream
    {
        /// <summary>
        /// The html closing tag to look for replacement.
        /// </summary>
        private const string BodyClosingTag = "</body>";

        /// <summary>
        /// Gets or sets the outpput <see cref="Stream"/> with the replaced html content.
        /// </summary>
        private Stream OutputStream { get; set; }

        /// <summary>
        /// Gets or set the current html content <see cref="Encoding"/>.
        /// </summary>
        private Encoding ContentEncoding { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Regex"/> object that will try match and replace the <see cref="BodyClosingTag"/>.
        /// </summary>
        private Regex BodyEndRegex { get; set; }

        /// <summary>
        /// Gets or set the yet unwritten <see cref="string"/> from the previous call to this stream.
        /// </summary>
        /// <remarks>Read more details on this at <see cref="Write"/> method</remarks>
        private string UnwrittenCharactersFromPreviousCall { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="ICodeInjector"/> that will inject html code on the <see cref="OutputStream"/>.
        /// </summary>
        private IEnumerable<Lazy<ICodeInjector>> CodeInjectors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastFilter"/> class.
        /// </summary>
        /// <param name="outputStream">The outpput <see cref="Stream"/> with the replaced html content.</param>
        /// <param name="contentEncoding">The current html content <see cref="Encoding"/>.</param>
        /// <param name="codeInjectors">The list of <see cref="ICodeInjector"/> that will inject html code on the <see cref="OutputStream"/>.</param>
        public ToastFilter(Stream outputStream, Encoding contentEncoding, IEnumerable<Lazy<ICodeInjector>> codeInjectors)
        {
            OutputStream = outputStream;
            ContentEncoding = contentEncoding;
            BodyEndRegex = new Regex(BodyClosingTag, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            CodeInjectors = codeInjectors;
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return OutputStream.CanRead; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return OutputStream.CanSeek; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return OutputStream.CanWrite; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get { return OutputStream.Length; }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get { return OutputStream.Position; }
            set { OutputStream.Position = value; }
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// Instead of calling this method, ensure that the stream is properly disposed.
        /// </summary>
        public override void Close()
        {
            OutputStream.Close();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return OutputStream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            OutputStream.SetLength(value);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the buffer contains the specified
        ///  byte array with the values between offset and (offset + count - 1) replaced
        ///  by the bytes read from the current source.
        ///  </param>
        /// <param name="offset">
        /// The zero-based byte offset in buffer at which to begin storing the data read
        /// from the current stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the
        /// number of bytes requested if that many bytes are not currently available,
        /// or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return OutputStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // There are different cases we need to deal with
            // Normally you would expect the contentInBuffer to contain the complete HTML code to return, but this is not always true because it is possible that 
            // the content that will be send back is larger than the buffer foreseen by ASP.NET (currently the buffer seems to be a little bit less than 16K)
            // and in that case this method will be called multiple times, which might result in false positives being written to the logs for not finding a </body> 
            // in the current chunk.

            // So we need to be able to deal with the following cases without writing those false positives
            // 1 - the </body> tag is found
            // 2 - the </body> tag was not found because
            //      2.1 - the </body> tag will be available in one of the next calls because the total length of the output is larger than 16K
            //      2.2 - the </body> tag is split up between this buffer and the next e.g.: "</bo" en "dy>"
            //      2.3 - the </body> tag will never be available (is missing)
            //      2.4 - Multiple </body> tags are available of which some might be part of a Javascript string or the markup is badly formatted

            // The easiest way to deal with this is to look for the last match for the </body> tag and if it is found we write everything before it to the
            // output stream and keep that </body> tag and everything that follows it (normally only a </html> tag but it can also be a 2.4 case) for the next call.
            // In case there is no match for the </body> tag, then we write everything to the output stream except for the last 10 characters (normally the last 6 would suffice, but we take a little margin to reassure us somehow ;-)) which we keep until the next call.

            // If there is a next call, then we first prepend the characters we kept from the previous call to the content inside the buffer (which might complete a chunked </body> tag for instance) 
            // and start our check all over again (which might result in finding a </body> tag or discarding a previously found </body> tag because that one was not the last one.
            // Anyhow, as long as we are not a the end and a </body> tag has been found previously, the output will be buffered, just to make sure there is no other </body> tag further down the stream.

            // If there is no next call, then the Flush method will be called and that one will deal with the current state, which means:
            // - in case there was a </body> tag found, the replacement will be done
            // - in case there was no </body> tag found, then the warning will be written to the log, indicating something went wrong
            // either way, the remaining unwritten characters will be sent down the output stream.
            string contentInBuffer = ContentEncoding.GetString(buffer, offset, count);

            // Prepend remaining characters from the previous call, if any
            if (!string.IsNullOrEmpty(UnwrittenCharactersFromPreviousCall))
            {
                contentInBuffer = UnwrittenCharactersFromPreviousCall + contentInBuffer;
                UnwrittenCharactersFromPreviousCall = null;
            }

            Match closingBodyTagMatch = BodyEndRegex.Match(contentInBuffer);
            if (closingBodyTagMatch.Success)
            {
                // Hooray, we found "a" </body> tag, but that doesn't mean that this is "the" last </body> tag we are looking for

                // so we write everything before that match to the output stream
                WriteToOutputStream(contentInBuffer.Substring(0, closingBodyTagMatch.Index));

                // and keep the remainder for the next call or the Flush if there is no next call
                UnwrittenCharactersFromPreviousCall = contentInBuffer.Substring(closingBodyTagMatch.Index);
            }
            else
            {
                // there is no match found for </body> which could have different reasons like case 2.2 for instance
                // therefor we'll write everything except the last 10 characters to the output stream and we'll keep the last 10 characters for the next call or the Flush method
                if (contentInBuffer.Length <= 10)
                {
                    // the content has a maximum length of 10 characters, so we don't need to write anything to the output stream and we'll keep those 
                    // characters for the next call (most likely the Flush)
                    UnwrittenCharactersFromPreviousCall = contentInBuffer;
                }
                else
                {
                    WriteToOutputStream(contentInBuffer.Substring(0, contentInBuffer.Length - 10));
                    UnwrittenCharactersFromPreviousCall = contentInBuffer.Substring(contentInBuffer.Length - 10);
                }
            }
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            if (!string.IsNullOrEmpty(UnwrittenCharactersFromPreviousCall))
            {
                string finalContentToWrite = UnwrittenCharactersFromPreviousCall;

                if (BodyEndRegex.IsMatch(UnwrittenCharactersFromPreviousCall))
                {
                    foreach (var injector in CodeInjectors)
                    {
                        // apparently we did seem to match a </body> tag, which means we can replace the last match with our HTML snippet
                        finalContentToWrite = BodyEndRegex.Replace(finalContentToWrite, injector.Value.GetCode() + BodyClosingTag, 1);
                    }
                }
                else
                {
                    // there was no </body> tag found, so we write down a warning to the log
                }

                // either way, if a replacement has been done or a warning has been written to the logs, the remaining unwritten characters must be written to the output stream
                WriteToOutputStream(finalContentToWrite);
            }

            OutputStream.Flush();
        }

        /// <summary>
        /// Appends the replaced html content to the <see cref="OutputStream"/>.
        /// </summary>
        /// <param name="content"></param>
        private void WriteToOutputStream(string content)
        {
            byte[] outputBuffer = ContentEncoding.GetBytes(content);
            OutputStream.Write(outputBuffer, 0, outputBuffer.Length);
        }
    }
}