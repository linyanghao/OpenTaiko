﻿namespace OpenTaiko {
	class CHitSounds {
		public CHitSounds(string path) {
			tLoadFile(path);
			for (int i = 0; i < 5; i++) {
				tReloadHitSounds(OpenTaiko.ConfigIni.nHitSounds[i], i);
			}
		}

		public bool tReloadHitSounds(int id, int player) {
			if (id >= names.Length || id >= data.Length)
				return false;

			string ext = this.data[id].format switch {
				"WAV" => ".wav",
				"OGG" => ".ogg",
				_ => ""
			};

			don[player] = data[id].path + "dong" + ext;
			ka[player] = data[id].path + "ka" + ext;
			adlib[player] = data[id].path + "Adlib" + ext;
			clap[player] = data[id].path + "clap" + ext;

			return true;
		}

		public string[] names;

		public string[] don = new string[5];
		public string[] ka = new string[5];
		public string[] adlib = new string[5];
		public string[] clap = new string[5];

		#region [private]

		private class HitSoundsData {
			public string name;
			public string path;
			public string format;
		}

		private HitSoundsData[] data;

		private void tLoadFile(string path) {
			data = ConfigManager.GetConfig<List<HitSoundsData>>(path).ToArray();
			names = new string[data.Length];
			for (int i = 0; i < data.Length; i++) {
				names[i] = data[i].name;
			}
		}

		#endregion
	}
}
