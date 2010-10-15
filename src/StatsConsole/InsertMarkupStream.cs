#region license
// Copyright 2010 Trafalgar Management Services Licensed under the Apache License,
// Version 2.0 (the "License"); you may not use this file except in compliance with the
// License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
// ANY KIND, either express or implied. See the License for the specific language governing
// permissions and limitations under the License. 
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace StatsConsole
{
    public class InsertMarkupStream : Stream
    {
        private Stream _original;
        private Encoding _encoding;
        private StreamWriter _streamWriter;
        
        public InsertMarkupStream(HttpResponseBase response)
        {
            _original = response.Filter;
            _encoding = response.ContentEncoding;
            _streamWriter = new StreamWriter(_original);
        }

        #region Other methods
        public override bool CanRead
        {
            get { return _original.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _original.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _original.CanWrite; }
        }

        public override void Flush()
        {
            _original.Flush();
        }

        public override long Length
        {
            get { return _original.Length; }
        }

        public override long Position
        {
            get { return _original.Position; }
            set { _original.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _original.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _original.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _original.SetLength(value);
        }

        #endregion

        private int _referenceIndex;
        private static readonly string ReferenceHead = "</head>".ToLowerInvariant();
        private static readonly string ReferenceBody = "</body>".ToLowerInvariant();
        readonly List<char> _bufferedCharacters = new List<char>();
        private bool _detectingTag;
        bool _initiallyDetectingTag;
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            var characters = _encoding.GetChars(buffer, offset, count);
            int startIndex = 0;
            int endIndex = 0;
            _initiallyDetectingTag = _detectingTag;

            for (int index = 0; index < characters.Length; index++)
            {
                if (_detectingTag)
                {
                    var lowerCaseCharacter = char.ToLowerInvariant(characters[index]);
                    _referenceIndex++;
                    if ((lowerCaseCharacter == ReferenceHead[_referenceIndex] || lowerCaseCharacter == ReferenceBody[_referenceIndex]))
                    {
                        if (lowerCaseCharacter == '>')
                        {
                            _streamWriter.Write(characters, startIndex, endIndex - startIndex);
                            startIndex = index;

                            string currentTag = string.Concat(_bufferedCharacters) + '>';
                            SignalTagDetected(currentTag);

                            _streamWriter.Write(_bufferedCharacters.ToArray());

                            ResetToNormal();
                        }
                    }
                    else
                    {
                        if (_initiallyDetectingTag)
                            _streamWriter.Write(_bufferedCharacters.ToArray());

                        ResetToNormal();
                    }
                }
                else if (characters[index] == '<')
                {
                    endIndex = index;
                    _detectingTag = true;
                }

                if (_detectingTag) _bufferedCharacters.Add(characters[index]);
            }

            endIndex = !_detectingTag ? characters.Length : endIndex;
            _streamWriter.Write(characters, startIndex, endIndex - startIndex);
            _streamWriter.Flush();
        }

        private void ResetToNormal()
        {
            _referenceIndex = 0;
            _detectingTag = _initiallyDetectingTag = false;
            _bufferedCharacters.Clear();
        }

        private bool _bodySignaled;
        private bool _headSignaled;
        public void SignalTagDetected(string tag)
        {
            if (string.Compare(ReferenceHead, tag, StringComparison.InvariantCultureIgnoreCase) == 0 && !_headSignaled)
            {
                var tagDetectedEvent = EndOfHeadDetected;
                if (tagDetectedEvent != null)
                    tagDetectedEvent(_streamWriter);

                _headSignaled = true;
            }

            if (string.Compare(ReferenceBody, tag, StringComparison.InvariantCultureIgnoreCase) == 0 && !_bodySignaled)
            {
                var tagDetectedEvent = EndOfBodyDetected;
                if (tagDetectedEvent != null)
                    tagDetectedEvent(_streamWriter);
                
                _bodySignaled = true;
            }
        }

        public event Action<StreamWriter> EndOfHeadDetected;
        public event Action<StreamWriter> EndOfBodyDetected;
    }
}