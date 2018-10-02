using UnityEngine;
using System.Collections;
using System.IO;
//using ProtoBuf;
//using ProtoBuf.Meta;

namespace npoi
{
	public class TextDataStream : FileStream
	{
		private const int HEAD_LEN = 8;

		private long m_iStartPos = 0;
		private long m_iEndPos = 0;

		private byte[] m_pLenBuff = new byte[8];

		public TextDataStream(string textPath)
			: base(textPath, FileMode.CreateNew, FileAccess.Write)
		{
		}

		public void Write<T>(T unit) where T : new()
		{
			m_iStartPos = Position;
			Write(m_pLenBuff, 0, HEAD_LEN);
            ProtoBuf.Serializer.Serialize<T>(this as Stream, unit);

			m_iEndPos = Position;

			long le = m_iEndPos - m_iStartPos - HEAD_LEN;
			char[] strbyte = le.ToString().ToCharArray();

			for (int jj = 0; jj < HEAD_LEN; jj++)
			{
				if (jj < strbyte.Length)
				{
					m_pLenBuff[jj] = (byte)strbyte[jj];
				}
				else
				{
					m_pLenBuff[jj] = 0;
				}
			}

			Seek(m_iStartPos, SeekOrigin.Begin);
			Write(m_pLenBuff, 0, HEAD_LEN);
			Seek(0, SeekOrigin.End);
		}

		public void Write(object unit)
		{
			m_iStartPos = Position;
			Write(m_pLenBuff, 0, HEAD_LEN);
            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(this as Stream, unit);

			m_iEndPos = Position;

			long le = m_iEndPos - m_iStartPos - HEAD_LEN;
			char[] strbyte = le.ToString().ToCharArray();

			for (int jj = 0; jj < HEAD_LEN; jj++)
			{
				if (jj < strbyte.Length)
				{
					m_pLenBuff[jj] = (byte)strbyte[jj];
				}
				else
				{
					m_pLenBuff[jj] = 0;
				}
			}

			Seek(m_iStartPos, SeekOrigin.Begin);
			Write(m_pLenBuff, 0, HEAD_LEN);
			Seek(0, SeekOrigin.End);
		}
	}
}