using System.Diagnostics;
using System.Text;

namespace EarlyCross.ScriptEncoder.Editor
{
	public static class EncodeClassifier
	{
		private const int ID_SHIFT_EUC = 932;
		private const int ID_EUC = 51932;
		private const int ID_JIS = 50220;
		private const byte BYTE_ESCAPE = 0x1B;
		private const byte BYTE_AT = 0x40;
		private const byte BYTE_DOLLAR = 0x24;
		private const byte BYTE_AND = 0x26;
		private const byte BYTE_OPEN = 0x28;
		private const byte BYTE_B = 0x42;
		private const byte BYTE_D = 0x44;
		private const byte BYTE_J = 0x4A;
		private const byte BYTE_I = 0x49;

		public static Encoding Classify(byte[] bytes)
		{
			int len = bytes.Length;

			for (int i = 0; i < len; i++)
			{
				byte b0 = bytes[i];

				if (b0 <= 0x06 || b0 == 0x7F || b0 == 0xFF)
				{
					if (b0 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7F)
					{
						return Encoding.Unicode;
					}
					else
					{
						return null;
					}
				}
			}

			bool notJapanese = true;
			foreach (byte b in bytes)
			{
				if (b == BYTE_ESCAPE || 0x80 <= b)
				{
					notJapanese = false;
					break;
				}
			}
			if (notJapanese)
			{
				return Encoding.ASCII;
			}

			for (int i = 0; i < len - 2; i++)
			{
				byte b0 = bytes[i];
				byte b1 = bytes[i + 1];
				byte b2 = bytes[i + 2];

				if (b0 == BYTE_ESCAPE)
				{
					if (b1 == BYTE_DOLLAR && b2 == BYTE_AT)
					{
						// JIS_0208 1978
						return Encoding.GetEncoding(ID_JIS);
					}
					else if (b1 == BYTE_DOLLAR && b2 == BYTE_B)
					{
						// JIS_0208 1983
						return Encoding.GetEncoding(ID_JIS);
					}
					else if (b1 == BYTE_OPEN && (b2 == BYTE_B || b2 == BYTE_J))
					{
						// JIS_ASC
						return Encoding.GetEncoding(ID_JIS);
					}
					else if (b1 == BYTE_OPEN && b2 == BYTE_I)
					{
						// JIS_KANA
						return Encoding.GetEncoding(ID_JIS);
					}

					if (i < len - 3)
					{
						byte b3 = bytes[i + 3];

						if (b1 == BYTE_DOLLAR && b2 == BYTE_OPEN && b3 == BYTE_D)
						{
							// JIS_0212
							return Encoding.GetEncoding(ID_JIS);
						}

						if (i < len - 5 && b1 == BYTE_AND && b2 == BYTE_AT && b3 == BYTE_ESCAPE && bytes[i + 4] == BYTE_DOLLAR && bytes[i + 5] == BYTE_B)
						{
							// JIS_0208 1990
							return Encoding.GetEncoding(ID_JIS);
						}
					}
				}
			}

			int sjis = 0;
			int euc = 0;
			int utf8 = 0;
			for (int i = 0; i < len - 1; i++)
			{
				byte b0 = bytes[i];
				byte b1 = bytes[i + 1];

				if (((0x81 <= b0 && b0 <= 0x9F) || (0xE0 <= b0 && b0 <= 0xFC)) && ((0x40 <= b1 && b1 <= 0x7E) || (0x80 <= b1 && b1 <= 0xFC)))
				{
					// SJIS_C
					sjis += 2;
					i++;
				}
			}

			for (int i = 0; i < len - 1; i++)
			{
				byte b0 = bytes[i];
				byte b1 = bytes[i + 1];

				if (((0xA1 <= b0 && b0 <= 0xFE) && (0xA1 <= b1 && b1 <= 0xFE)) || (b0 == 0x8E && (0xA1 <= b1 && b1 <= 0xDF)))
				{
					// EUC_C
					// EUC_KANA
					euc += 2;
					i++;
				}
				else if (i < len - 2)
				{
					byte b2 = bytes[i + 2];

					if (b0 == 0x8F && (0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE))
					{
						// EUC_0212
						euc += 3;
						i += 2;
					}
				}
			}

			for (int i = 0; i < len - 1; i++)
			{
				byte b0 = bytes[i];
				byte b1 = bytes[i + 1];

				if ((0xC0 <= b0 && b0 <= 0xDF) && (0x80 <= b1 && b1 <= 0xBF))
				{
					// UTF8
					utf8 += 2;
					i++;
				}
				else if (i < len - 2)
				{
					byte b2 = bytes[i + 2];

					if ((0xE0 <= b0 && b0 <= 0xEF) && (0x80 <= b1 && b1 <= 0xBF) && (0x80 <= b2 && b2 <= 0xBF))
					{
						// UTF8
						utf8 += 3;
						i += 2;
					}
				}
			}

			Debug.WriteLine(string.Format("sjis = {0}, euc = {1}, utf8 = {2}", sjis, euc, utf8));

			if (euc > sjis && euc > utf8)
			{
				// EUC
				return Encoding.GetEncoding(ID_EUC);
			}
			else if (sjis > euc && sjis > utf8)
			{
				// SJIS
				return Encoding.GetEncoding(ID_SHIFT_EUC);
			}
			else if (utf8 > euc && utf8 > sjis)
			{
				// UTF8
				return Encoding.UTF8;
			}

			return null;
		}
	}
}
