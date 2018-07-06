/*
    Copyright 2014 Rustici Software

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
using System;

namespace TinCan.LRSResponses
{
	//
	// this isn't abstract because some responses for an LRS won't have content
	// so in those cases we can get by just returning this base response
	//
	public class LRSResponse
	{
		public bool success { get; set; }
		public Exception httpException { get; set; }
		public LRSResponseError? Error { get; protected set; }
		public string errMsg
		{
			get
			{
				if (Error == null)
				{
					return null;
				}

				return Error?.Message;
			}
		}

		public void SetErrMsgFromBytes(byte[] content, int code = -1)
		{
			string message = null;
			if (content != null)
			{
				message = System.Text.Encoding.UTF8.GetString(content, 0, content.Length);
			}
			Error = new LRSResponseError(message, code);
		}

		public override string ToString()
		{
			return string.Format("[LRSResponse: success={0}, httpException={1}, errMsg={2}]", success, httpException, errMsg);
		}
	}

	public struct LRSResponseError
	{
		public readonly string Message;
		public readonly int Code;

		public LRSResponseError(string message, int code = -1)
		{
			Message = message;
			Code = code;
		}
	}
}
