using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EarlyCross.ScriptEncoder.Editor
{
	public class ScriptEncoder : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] _0, string[] _1, string[] _2)
		{
			foreach (string asset in importedAssets)
			{
				if (!File.Exists(asset))
				{
					continue;
				}

				string ext = Path.GetExtension(asset);
				if (ext != ".cs")
				{
					continue;
				}

				// ファイルを開く
				FileStream fs = new FileStream(asset, FileMode.Open, FileAccess.Read);
				byte[] bs = new byte[fs.Length];
				fs.Read(bs, 0, bs.Length);
				fs.Close();

				// エンコードを取得
				var enc = EncodeClassifier.Classify(bs);
				if (enc == null)
				{
					continue;
				}

				if (IsUtf8WithBom(enc, bs))
				{
					continue;
				}

				// 改行コードの置き換え
				string contents = enc.GetString(bs);
				contents = Regex.Replace(contents, "([^\r])\n", "$1\r\n");

				// ファイルを保存
				File.WriteAllText(asset, contents, Encoding.GetEncoding("utf-8"));
				Debug.LogWarning("Convert script encode to UTF-8 with BOM : " + asset);
			}
		}

		private static bool IsUtf8WithBom(Encoding enc, byte[] bytes)
		{
			if (enc.CodePage != 65001)
			{
				return false;
			}

			if ((bytes[0] == 0xEF) && (bytes[1] == 0xBB) && (bytes[2] == 0xBF))
			{
				return true;
			}

			return false;
		}
	}
}
