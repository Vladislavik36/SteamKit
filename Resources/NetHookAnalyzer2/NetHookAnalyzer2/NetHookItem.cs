﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using ProtoBuf;
using SteamKit2;
using SteamKit2.Internal;

namespace NetHookAnalyzer2
{
	class NetHookItem
	{
		public enum PacketDirection 
		{
			In,
			Out
		}

		static Regex NameRegex = new Regex(
			@"(?<num>\d+)_(?<direction>in|out)_(?<emsg>\d+)_k_EMsg(?<name>[\w_<>]+)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase
		);

		public string Name { get; private set; }
		public int Sequence { get; private set; }
		public PacketDirection Direction { get; private set; }
		public EMsg EMsg { get; private set; }

		public string InnerMessageName
		{
			get { return innerMessageName ?? (innerMessageName = ReadInnerMessageName()); }
		}
		string innerMessageName;

		public FileInfo FileInfo { get; private set; }

		public Stream OpenStream()
		{
			return FileInfo.OpenRead();
		}
		
		public bool LoadFromFile(FileInfo fileInfo)
		{
			Match m = NameRegex.Match( fileInfo.Name );

			if ( !m.Success )
			{
				return false;
			}

			int sequence;
			if (!int.TryParse(m.Groups["num"].Value, out sequence))
			{
				return false;
			}

			var direction = m.Groups[ "direction" ].Value;
			PacketDirection packetDirection;
			if (!Enum.TryParse<PacketDirection>(direction, ignoreCase: true, result: out packetDirection))
			{
				return false;
			}

			uint emsg;
			if (!uint.TryParse(m.Groups["emsg"].Value, out emsg))
			{
				return false;
			}

			FileInfo = fileInfo;

			Sequence = sequence;
			Direction = packetDirection;
			EMsg = (EMsg)emsg;
			Name = m.Groups["name"].Value;

			return true;
		}

		string ReadInnerMessageName()
		{
			try
			{
				return ReadInnerMessageNameCore();
			}
			catch (IOException)
			{
				return null;
			}
		}

		string ReadInnerMessageNameCore()
		{
			switch (EMsg)
			{
				case SteamKit2.EMsg.ClientToGC:
				case SteamKit2.EMsg.ClientFromGC:
				{
					var proto = ReadAsProtobufMsg<CMsgGCClient>();
					var gcEMsg = proto.Body.msgtype;
					var gcName = EMsgExtensions.GetGCMessageName(gcEMsg, proto.Body.appid);

					var headerToTrim = "k_EMsg";
					if (gcName.StartsWith(headerToTrim))
					{
						gcName = gcName.Substring(headerToTrim.Length);
					}

					return gcName;
				}

				case SteamKit2.EMsg.ServiceMethod:
				{
					var fileData = File.ReadAllBytes(FileInfo.FullName);
					var hdr = new MsgHdrProtoBuf();
					using (var ms = new MemoryStream(fileData))
					{
						hdr.Deserialize(ms);
					}

					return hdr.Proto.target_job_name;
				}

				case SteamKit2.EMsg.ClientServiceMethod:
				{
					var proto = ReadAsProtobufMsg<CMsgClientServiceMethod>();
					return proto.Body.method_name;
				}

				case SteamKit2.EMsg.ClientServiceMethodResponse:
				{
					var proto = ReadAsProtobufMsg<CMsgClientServiceMethodResponse>();
					return proto.Body.method_name;
				}

				default:
					return string.Empty;

			}
		}

		ClientMsgProtobuf<T> ReadAsProtobufMsg<T>()
			where T : IExtensible, new()
		{
			var fileData = File.ReadAllBytes(FileInfo.FullName);
			var msg = new SteamKit2.PacketClientMsgProtobuf(EMsg, fileData);
			var proto = new ClientMsgProtobuf<T>(msg);
			return proto;
		}
	}
}
