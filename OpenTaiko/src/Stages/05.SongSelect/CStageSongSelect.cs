﻿using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using DiscordRPC;
using FDK;

namespace OpenTaiko;

/*
 ** class CSongSelectSongManager
 ** playSongIfPossible : Play song is enabled and not playing, supports both intro and regular song
 ** stopSong : Stop song without disabling it
 ** enable : Enable the menu song
 ** disable : Disable the menu song
 */

#region [Generic static class for handling the menu song]

static internal class CSongSelectSongManager {
	public static CSkin.CSystemSound bgmIn {
		get {
			if (OpenTaiko.ConfigIni.bAIBattleMode) {
				return OpenTaiko.Skin.bgmSongSelect_AI_In;
			} else {
				return OpenTaiko.Skin.bgm選曲画面イン;
			}
		}
	}
	public static CSkin.CSystemSound bgmLoop {
		get {
			if (OpenTaiko.ConfigIni.bAIBattleMode) {
				return OpenTaiko.Skin.bgmSongSelect_AI;
			} else {
				return OpenTaiko.Skin.bgm選曲画面;
			}
		}
	}

	public static void playSongIfPossible() {
		if (CSongSelectSongManager.isSongDisabled)
			return;

		if (OpenTaiko.ConfigIni.bBGMPlayVoiceSound && !bgmIn.bIsPlaying && !bgmLoop.bIsPlaying) {
			if (inSongPlayed == false) {
				bgmIn.tPlay();
				CSongSelectSongManager.inSongPlayed = true;
			} else {
				bgmLoop.tPlay();
			}
		}

	}

	public static void stopSong() {
		bgmIn.tStop();
		bgmLoop.tStop();
		CSongSelectSongManager.inSongPlayed = false;
	}

	public static void enable() {
		CSongSelectSongManager.isSongDisabled = false;
	}

	public static void disable() {
		CSongSelectSongManager.stopSong();
		CSongSelectSongManager.isSongDisabled = true;
	}


	private static bool inSongPlayed = false;
	private static bool isSongDisabled = false;
}

#endregion

internal class CStageSongSelect : CStage {
	// Properties
	public int nスクロールバー相対y座標 {
		get {
			if (actSongList != null) {
				return actSongList.nスクロールバー相対y座標;
			} else {
				return 0;
			}
		}
	}
	public bool bIsEnumeratingSongs {
		get {
			return actSongList.bIsEnumeratingSongs;
		}
		set {
			actSongList.bIsEnumeratingSongs = value;
		}
	}
	public bool bCurrentlyScrolling {
		get {
			return this.actSongList.bスクロール中;
		}
	}
	public int[] nChoosenSongDifficulty = new int[5];

	public string str確定された曲のジャンル {
		get;
		set;
	}
	public CScore r確定されたスコア {
		get;
		set;
	}
	public CSongListNode rChoosenSong {
		get;
		set;
	}
	public int n現在選択中の曲の難易度 {
		get {
			return this.actSongList.n現在選択中の曲の現在の難易度レベル;
		}
	}
	public CScore r現在選択中のスコア {
		get {
			return this.actSongList.r現在選択中のスコア;
		}
	}
	public CSongListNode rPrevSelectedSong {
		get {
			return this.actSongList.rPrevSelectedSong;
		}
	}
	public CSongListNode rNowSelectedSong {
		get {
			return this.actSongList.rCurrentlySelectedSong;
		}
		set {
			this.actSongList.rCurrentlySelectedSong = value;
		}
	}

	// コンストラクタ
	public CStageSongSelect() {
		base.eStageID = CStage.EStage.SongSelect;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		base.IsDeActivated = true;
		base.ChildActivities.Add(this.actオプションパネル = new CActオプションパネル());
		base.ChildActivities.Add(this.actFIFO = new CActFIFOBlack());
		base.ChildActivities.Add(this.actFIfrom結果画面 = new CActFIFOBlack());
		//base.list子Activities.Add( this.actFOtoNowLoading = new CActFIFOBlack() );
		base.ChildActivities.Add(this.actFOtoNowLoading = new CActFIFOStart());
		base.ChildActivities.Add(this.actSongList = new CActSelect曲リスト());
		base.ChildActivities.Add(this.actSongInfo = new CActSelectSongInfo());
		base.ChildActivities.Add(this.actDanInfo = new CActSelectDanInfo());
		base.ChildActivities.Add(this.actTowerInfo = new CActSelectTowerInfo());
		base.ChildActivities.Add(this.actステータスパネル = new CActSelectステータスパネル());
		base.ChildActivities.Add(this.act演奏履歴パネル = new CActSelect演奏履歴パネル());
		base.ChildActivities.Add(this.actPreimageパネル = new CActSelectPreimageパネル());
		base.ChildActivities.Add(this.actPresound = new CActSelectPresound());
		base.ChildActivities.Add(this.actArtistComment = new CActSelectArtistComment());
		base.ChildActivities.Add(this.actInformation = new CActSelectInformation());
		base.ChildActivities.Add(this.actSortSongs = new CActSortSongs());
		base.ChildActivities.Add(this.actShowCurrentPosition = new CActSelectShowCurrentPosition());
		base.ChildActivities.Add(this.actQuickConfig = new CActSelectQuickConfig());
		base.ChildActivities.Add(this.actDifficultySelectionScreen = new CActSelect難易度選択画面());
		base.ChildActivities.Add(this.actPlayOption = new CActPlayOption());
		base.ChildActivities.Add(this.actExExtraTransAnime = new CActSelectExExtraTransAnime());
		base.ChildActivities.Add(this.actNewHeya = new CActNewHeya());
		base.ChildActivities.Add(this.PuchiChara = new PuchiChara());


		for (int i = 0; i < 10; i++) {
			stSongNumber[i].ch = i.ToString().ToCharArray()[0];
			stSongNumber[i].pt = new Point(27 * i, 0);
		}

		for (int i = 0; i < 10; i++) {
			stBoardNumber[i].ch = i.ToString().ToCharArray()[0];
			stBoardNumber[i].pt = new Point(15 * i, 0);
		}

		this.CommandHistory = new CCommandHistory();        // #24063 2011.1.16 yyagi
	}


	// メソッド

	public void tNotifySelectedSongChange() {
		int scroll = this.ct背景スクロール用タイマー.CurrentValue;

		if (rPrevSelectedSong != null) {
			bool bchangedBGPath = rNowSelectedSong != null && rNowSelectedSong.strSelectBGPath != rPrevSelectedSong.strSelectBGPath;

			if (bchangedBGPath)
				OpenTaiko.tテクスチャの解放(ref txCustomPrevSelectBG);

			txCustomPrevSelectBG = txCustomSelectBG;

			if (bchangedBGPath) {
				if (rNowSelectedSong.strSelectBGPath != null && rNowSelectedSong.strSelectBGPath != "") {
					txCustomSelectBG = OpenTaiko.tテクスチャの生成(rNowSelectedSong.strSelectBGPath);
				} else {
					txCustomSelectBG = null;
				}
			}
		}

		float scale = OpenTaiko.Skin.Resolution[1] / (float)txGenreBack.szTextureSize.Height;
		this.ct背景スクロール用タイマー = new CCounter(0, (int)(txGenreBack.szTextureSize.Width * scale), 30, OpenTaiko.Timer);
		this.ct背景スクロール用タイマー.CurrentValue = Math.Min(scroll, (int)(txGenreBack.szTextureSize.Width * scale));

		float oldScale = OpenTaiko.Skin.Resolution[1] / (float)txOldGenreBack.szTextureSize.Height;
		this.ctOldBGScroll = new CCounter(0, (int)(txOldGenreBack.szTextureSize.Width * oldScale), 30, OpenTaiko.Timer);
		this.ctOldBGScroll.CurrentValue = Math.Min(scroll, (int)(txOldGenreBack.szTextureSize.Width * oldScale));

		this.actPreimageパネル.tSelectedSongChanged();
		this.actPresound.t選択曲が変更された();
		this.act演奏履歴パネル.t選択曲が変更された();
		this.actステータスパネル.t選択曲が変更された();
		this.actArtistComment.t選択曲が変更された();
		actDanInfo.UpdateSong();

		#region [ プラグインにも通知する（BOX, RANDOM, BACK なら通知しない）]
		//---------------------
		if (OpenTaiko.app != null) {
			var c曲リストノード = OpenTaiko.stageSongSelect.rNowSelectedSong;
			var cスコア = OpenTaiko.stageSongSelect.r現在選択中のスコア;

			if (c曲リストノード != null && cスコア != null && c曲リストノード.nodeType == CSongListNode.ENodeType.SCORE) {
				string str選択曲ファイル名 = cスコア.ファイル情報.ファイルの絶対パス;
				int n曲番号inブロック = OpenTaiko.stageSongSelect.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(c曲リストノード);
			}
		}
		//---------------------
		#endregion
	}

	// CStage 実装

	/// <summary>
	/// 曲リストをリセットする
	/// </summary>
	/// <param name="cs"></param>
	public void Refresh(CSongs管理 cs, bool bRemakeSongTitleBar) {
		this.actSongList.Refresh(cs, bRemakeSongTitleBar);
	}

	public override void Activate() {
		Trace.TraceInformation("選曲ステージを活性化します。");
		Trace.Indent();
		try {
			nChoosenSongDifficulty = new int[5];
			this.eフェードアウト完了時の戻り値 = EReturnValue.継続;

			// BGM played
			this.bBGM再生済み = false;

			this.ct背景スクロール用タイマー = new CCounter(0, txGenreBack.szTextureSize.Width, 30, OpenTaiko.Timer);
			this.ctOldBGScroll = new CCounter(0, txOldGenreBack.szTextureSize.Width, 30, OpenTaiko.Timer);

			OpenTaiko.Skin.voiceMenuSongSelect[OpenTaiko.SaveFile]?.tPlay();

			for (int i = 0; i < 2; i++)
				this.ctキー反復用[i] = new CCounter(0, 0, 0, OpenTaiko.Timer);

			//ctChara_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Chara_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);
			CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL);

			//ctChara_Select = new CCounter();
			//ctChara_Jump[0] = new CCounter();
			//ctChara_Jump[1] = new CCounter();

			CMenuCharacter.tDisableCounter(CMenuCharacter.ECharacterAnimation.SELECT);
			CMenuCharacter.tDisableCounter(CMenuCharacter.ECharacterAnimation.START);

			ctBackgroundFade = new CCounter();
			ctCreditAnime = new CCounter(0, 4500, 1, OpenTaiko.Timer);
			ctTimer = new CCounter(0, 100, 1000, OpenTaiko.Timer);

			this.PuchiChara.IdleAnimation();

			ctBackgroundFade.CurrentValue = 600;

			/*
            if(TJAPlayer3.ConfigIni.bBGM音を発声する && !TJAPlayer3.Skin.bgm選曲画面イン.b再生中 && !TJAPlayer3.Skin.bgm選曲画面.b再生中)
                TJAPlayer3.Skin.bgm選曲画面イン.t再生する();
            */


			for (int i = 0; i < 3; i++)
				r[i] = new Random();

			//this.act難易度選択画面.bIsDifficltSelect = true;
			base.Activate();

			this.actステータスパネル.t選択曲が変更された();  // 最大ランクを更新
											// Discord Presenceの更新
			OpenTaiko.DiscordClient?.SetPresence(new RichPresence() {
				Details = "",
				State = "SongSelect",
				Timestamps = new Timestamps(OpenTaiko.StartupTime),
				Assets = new Assets() {
					LargeImageKey = OpenTaiko.LargeImageKey,
					LargeImageText = OpenTaiko.LargeImageText,
				}
			});



			if (rNowSelectedSong != null)
				NowGenre = rNowSelectedSong.songGenre;

			AI_Background = new ScriptBG(CSkin.Path($@"{TextureLoader.BASE}{TextureLoader.SONGSELECT}{Path.DirectorySeparatorChar}AIBattle{Path.DirectorySeparatorChar}Script.lua"));
			AI_Background.Init();
		} finally {
			Trace.TraceInformation("選曲ステージの活性化を完了しました。");
			Trace.Unindent();
		}
	}
	public override void DeActivate() {
		Trace.TraceInformation("選曲ステージを非活性化します。");
		Trace.Indent();
		try {
			for (int i = 0; i < 2; i++) {
				this.ctキー反復用[i] = null;
			}

			OpenTaiko.tDisposeSafely(ref AI_Background);

			base.DeActivate();
		} finally {
			Trace.TraceInformation("選曲ステージの非活性化を完了しました。");
			Trace.Unindent();
		}
	}
	public override void CreateManagedResource() {
		this.ftフォント = new CCachedFontRenderer(CFontRenderer.DefaultFontName, 26, CFontRenderer.FontStyle.Regular);

		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		if (this.ftフォント != null) {
			this.ftフォント.Dispose();
			this.ftフォント = null;
		}

		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {
			this.ct背景スクロール用タイマー.TickLoop();
			this.ctOldBGScroll.TickLoop();
			#region [ 初めての進行描画 ]
			//---------------------
			if (base.IsFirstDraw) {
				this.ct登場時アニメ用共通 = new CCounter(0, 100, 3, OpenTaiko.Timer);
				if (OpenTaiko.rPreviousStage == OpenTaiko.stageResults) {
					this.actFIfrom結果画面.tフェードイン開始();
					base.ePhaseID = CStage.EPhase.SongSelect_FadeInFromResults;
				} else {
					this.actFIFO.tフェードイン開始();
					base.ePhaseID = CStage.EPhase.Common_FADEIN;
				}
				this.tNotifySelectedSongChange();
				base.IsFirstDraw = false;
			}
			//---------------------
			#endregion


			ctTimer.Tick();
			ctCreditAnime.TickLoop();
			ctBackgroundFade.Tick();

			//ctChara_Select.t進行();
			//ctChara_Jump[0].t進行();
			//ctChara_Jump[1].t進行();
			//ctChara_Normal.t進行Loop();

			this.ct登場時アニメ用共通.Tick();

			if (OpenTaiko.ConfigIni.bAIBattleMode) {
				AI_Background?.Update();
				AI_Background?.Draw();
			} else {
				if (OpenTaiko.Tx.SongSelect_Background != null)
					OpenTaiko.Tx.SongSelect_Background.t2D描画(0, 0);
			}

			if (this.rNowSelectedSong != null) {

				#region [Background]

				nGenreBack = this.NowBg;
				nOldGenreBack = this.OldBg;

				if (!OpenTaiko.ConfigIni.bAIBattleMode) {
					if (txGenreBack != null) {
						float scale = OpenTaiko.Skin.Resolution[1] / (float)txGenreBack.szTextureSize.Height;
						for (int i = 0; i < (OpenTaiko.Skin.Resolution[0] / (txGenreBack.szTextureSize.Width * scale)) + 2; i++) {
							if (txGenreBack != null) {
								txGenreBack.color4 = CConversion.ColorToColor4(this.NowBgColor);
								txGenreBack.Opacity = 255;
								txGenreBack.vcScaleRatio.X = scale;
								txGenreBack.vcScaleRatio.Y = scale;
								txGenreBack.t2D描画(-(int)ct背景スクロール用タイマー.CurrentValue + (txGenreBack.szTextureSize.Width * scale) * i, 0);
							}
						}
					}
					if (txOldGenreBack != null) {
						float scale = OpenTaiko.Skin.Resolution[1] / (float)txOldGenreBack.szTextureSize.Height;
						for (int i = 0; i < (OpenTaiko.Skin.Resolution[0] / (txOldGenreBack.szTextureSize.Width * scale)) + 2; i++) {
							if (txOldGenreBack != null) {
								txOldGenreBack.color4 = CConversion.ColorToColor4(this.OldBgColor);
								txOldGenreBack.Opacity = 600 - ctBackgroundFade.CurrentValue;
								txOldGenreBack.vcScaleRatio.X = scale;
								txOldGenreBack.vcScaleRatio.Y = scale;
								txOldGenreBack.t2D描画(-(int)ctOldBGScroll.CurrentValue + (txOldGenreBack.szTextureSize.Width * scale) * i, 0);
							}
						}
					}
				}

				#endregion

				#region [Song Panel]

				if (this.rNowSelectedSong.nodeType == CSongListNode.ENodeType.BOX) {
					OpenTaiko.Tx.SongSelect_Song_Panel[0]?.t2D描画(0, 0);
				} else if (this.rNowSelectedSong.nodeType == CSongListNode.ENodeType.SCORE) {
					var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(this.rNowSelectedSong);
					var HiddenIndex = OpenTaiko.Databases.DBSongUnlockables.tGetSongHiddenIndex(this.rNowSelectedSong);

					if (HiddenIndex >= DBSongUnlockables.EHiddenIndex.GRAYED) {
						OpenTaiko.Tx.SongSelect_Song_Panel[4]?.t2D描画(0, 0);
					} else {
						if (OpenTaiko.stageSongSelect.n現在選択中の曲の難易度 == (int)Difficulty.Dan)
							OpenTaiko.Tx.SongSelect_Song_Panel[2]?.t2D描画(0, 0);
						else if (OpenTaiko.stageSongSelect.n現在選択中の曲の難易度 == (int)Difficulty.Tower)
							OpenTaiko.Tx.SongSelect_Song_Panel[3]?.t2D描画(0, 0);
						else
							OpenTaiko.Tx.SongSelect_Song_Panel[1]?.t2D描画(0, 0);
					}
				}

				#endregion
			}

			this.actSongList.Draw();
			int y = 0;
			if (this.ct登場時アニメ用共通.IsTicked) {
				double db登場割合 = ((double)this.ct登場時アニメ用共通.CurrentValue) / 100.0;   // 100が最終値
				double dbY表示割合 = Math.Sin(Math.PI / 2 * db登場割合);
				y = ((int)(OpenTaiko.Tx.SongSelect_Header.sz画像サイズ.Height * dbY表示割合)) - OpenTaiko.Tx.SongSelect_Header.sz画像サイズ.Height;
			}

			OpenTaiko.Tx.SongSelect_Header?.t2D描画(0, 0);
			OpenTaiko.Tx.SongSelect_Footer?.t2D描画(0, 0);

			tTimerDraw(100 - ctTimer.CurrentValue);

			#region [Song Info]

			if (this.rNowSelectedSong != null) {
				if (this.rNowSelectedSong.nodeType == CSongListNode.ENodeType.BOX) {
				} else if (this.rNowSelectedSong.nodeType == CSongListNode.ENodeType.SCORE) {
					var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(this.rNowSelectedSong);
					var HiddenIndex = OpenTaiko.Databases.DBSongUnlockables.tGetSongHiddenIndex(this.rNowSelectedSong);

					if (HiddenIndex < DBSongUnlockables.EHiddenIndex.GRAYED) {
						actSongInfo.Draw();
						if (this.n現在選択中の曲の難易度 == (int)Difficulty.Dan) {
							actDanInfo.Draw();
						} else if (this.n現在選択中の曲の難易度 == (int)Difficulty.Tower) {
							actTowerInfo.Draw();
						} else {
						}
					}
				}
			}

			#endregion

			tSongNumberDraw(OpenTaiko.Skin.SongSelect_SongNumber_X[0], OpenTaiko.Skin.SongSelect_SongNumber_Y[0], NowSong);
			tSongNumberDraw(OpenTaiko.Skin.SongSelect_SongNumber_X[1], OpenTaiko.Skin.SongSelect_SongNumber_Y[1], MaxSong);

			this.actInformation.Draw();

			#region[Modicons]

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				ModIcons.tDisplayModsMenu(OpenTaiko.Skin.SongSelect_ModIcons_X[i], OpenTaiko.Skin.SongSelect_ModIcons_Y[i], i);
			}

			int dyGameMode = OpenTaiko.actTextConsole.fontHeight;
			int yGameMode = 0;
			if (OpenTaiko.ConfigIni.bTokkunMode)
				OpenTaiko.actTextConsole.Print(0, yGameMode, CTextConsole.EFontType.White, "GAME: TRAINING MODE");
			yGameMode += dyGameMode;
			if (OpenTaiko.ConfigIni.eGameMode == EGame.Survival)
				OpenTaiko.actTextConsole.Print(0, yGameMode, CTextConsole.EFontType.White, "GAME: SURVIVAL");
			if (OpenTaiko.ConfigIni.eGameMode == EGame.SurvivalHard)
				OpenTaiko.actTextConsole.Print(0, yGameMode, CTextConsole.EFontType.White, "GAME: SURVIVAL HARD");
			yGameMode += dyGameMode;
			if (OpenTaiko.ConfigIni.bSuperHard)
				OpenTaiko.actTextConsole.Print(0, yGameMode, CTextConsole.EFontType.Cyan, "SUPER HARD MODE : ON");

			#endregion

			#region [Preimage, upper lock layer and unlock conditions]

			if (this.rNowSelectedSong != null
				&& this.rNowSelectedSong.nodeType == CSongListNode.ENodeType.SCORE) {
				var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(this.rNowSelectedSong);
				var HiddenIndex = OpenTaiko.Databases.DBSongUnlockables.tGetSongHiddenIndex(this.rNowSelectedSong);

				if (this.actDifficultySelectionScreen.bIsDifficltSelect == false || this.actSongList.ctDifficultyIn.CurrentValue < 1000)
					this.actPreimageパネル.Draw();

				if (HiddenIndex >= DBSongUnlockables.EHiddenIndex.GRAYED)
					OpenTaiko.Tx.SongSelect_Song_Panel[5]?.t2D描画(0, 0);

				if (IsSongLocked) {
					OpenTaiko.Tx.SongSelect_Unlock_Conditions?.t2D描画(0, 0);

					if (actSongList.ttkNowUnlockConditionText is not null) {
						TitleTextureKey.ResolveTitleTexture(actSongList.ttkNowUnlockConditionText)?.t2D描画(OpenTaiko.Skin.SongSelect_Unlock_Conditions_Text[0], OpenTaiko.Skin.SongSelect_Unlock_Conditions_Text[1]);
					}
				}
			}

			#endregion

			this.actPresound.Draw();

			this.act演奏履歴パネル.Draw();

			this.actShowCurrentPosition.Draw();                               // #27648 2011.3.28 yyagi

			// Select screen song
			if (base.ePhaseID == CStage.EPhase.Common_NORMAL) {
				CSongSelectSongManager.playSongIfPossible();
			}

			if (this.ctDiffSelect移動待ち != null)
				this.ctDiffSelect移動待ち.Tick();


			#region [Character & PuchiChara]


			//if (this.ctChara_Select.b終了値に達してない)

			for (int player = 0; player < OpenTaiko.ConfigIni.nPlayerCount; player++) {
				CCounter ___cc = CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.SELECT)[player];

				int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;

				//int chara_x = TJAPlayer3.Skin.Characters_Menu_X[_charaId][player];
				//int chara_y = TJAPlayer3.Skin.Characters_Menu_Y[_charaId][player];

				int chara_x = OpenTaiko.Skin.SongSelect_NamePlate_X[player] + OpenTaiko.Tx.NamePlateBase.szTextureSize.Width / 2;
				int chara_y = OpenTaiko.Skin.SongSelect_NamePlate_Y[player];

				//int puchi_x = player == 0 ? 0 + 100 : 981 + 250;
				//int puchi_y = player == 0 ? 330 + 230 : 330 + 230;

				int puchi_x = chara_x + OpenTaiko.Skin.Adjustments_MenuPuchichara_X[player % 2];
				int puchi_y = chara_y + OpenTaiko.Skin.Adjustments_MenuPuchichara_Y[player % 2];

				if (___cc != null && ___cc.IsUnEnded) {
					CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.SELECT);

					this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
				} else {
					CCounter ___cj = CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.START)[player];


					if (___cj != null && ___cj.EndValue > 0) {
						CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.START);

						this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
					} else if (actDifficultySelectionScreen.bIsDifficltSelect && actDifficultySelectionScreen.bSelect[player]) {
						CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.WAIT);

						this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
					} else {
						CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.NORMAL);

						this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
					}
				}
			}


			#endregion

			#region [ Nameplate ]
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				OpenTaiko.NamePlate.tNamePlateDraw(OpenTaiko.Skin.SongSelect_NamePlate_X[i], OpenTaiko.Skin.SongSelect_NamePlate_Y[i], i);
			}
			#endregion

			#region [Pad displayables]

			int defaultTable = Math.Max(0, Math.Min((int)Difficulty.Edit + 1, OpenTaiko.ConfigIni.nDefaultCourse));

			int[] currentPads = new int[5] {
				defaultTable,
				defaultTable,
				defaultTable,
				defaultTable,
				defaultTable };

			//int currentPad = (int)Difficulty.Edit + 1;
			if (OpenTaiko.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect) {
				if (OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[0] >= 2)
					currentPads[0] = OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[0] - 2;
				if (OpenTaiko.ConfigIni.nPlayerCount > 1 && OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[1] >= 2)
					currentPads[1] = OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[1] - 2;
				if (OpenTaiko.ConfigIni.nPlayerCount > 1 && OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[2] >= 2)
					currentPads[2] = OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[2] - 2;
				if (OpenTaiko.ConfigIni.nPlayerCount > 1 && OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[3] >= 2)
					currentPads[3] = OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[3] - 2;
				if (OpenTaiko.ConfigIni.nPlayerCount > 1 && OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[4] >= 2)
					currentPads[4] = OpenTaiko.stageSongSelect.actDifficultySelectionScreen.n現在の選択行[4] - 2;
			}



			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

				int p = OpenTaiko.GetActualPlayer(i);

				OpenTaiko.Tx.SongSelect_Table[currentPads[i]]?.t2D描画(OpenTaiko.Skin.SongSelect_Table_X[i], OpenTaiko.Skin.SongSelect_Table_Y[i]);

				CActSelect曲リスト.CScorePad[] SPArrRef = CSongDict.ScorePads[p];

				// Current board
				for (int j = 0; j < 11; j++) {
					tBoardNumberDraw(OpenTaiko.Skin.SongSelect_BoardNumber_X[i][j], OpenTaiko.Skin.SongSelect_BoardNumber_Y[i][j], j < 7 ?
						SPArrRef[currentPads[i]].ScoreRankCount[j]
						: SPArrRef[currentPads[i]].CrownCount[j - 7]);
				}

			}

			if (OpenTaiko.ConfigIni.nPlayerCount <= 2) {
				OpenTaiko.Tx.SongSelect_Coin_Slot[0]?.t2D描画(0, 0,
					new Rectangle(0, 0, (OpenTaiko.Tx.SongSelect_Coin_Slot[0].sz画像サイズ.Width / 2) + ((OpenTaiko.ConfigIni.nPlayerCount > 1 && !OpenTaiko.ConfigIni.bAIBattleMode) ? (OpenTaiko.Tx.SongSelect_Coin_Slot[0].sz画像サイズ.Width / 2) : 0), OpenTaiko.Tx.SongSelect_Coin_Slot[0].sz画像サイズ.Height));
			} else {
				OpenTaiko.Tx.SongSelect_Coin_Slot[OpenTaiko.ConfigIni.nPlayerCount - 2]?.t2D描画(0, 0);
			}

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

				int p = OpenTaiko.GetActualPlayer(i);

				if (OpenTaiko.SaveFileInstances[p].data.Medals >= 0)
					tBoardNumberDraw(OpenTaiko.Skin.SongSelect_BoardNumber_X[i][11], OpenTaiko.Skin.SongSelect_BoardNumber_Y[i][11], (int)OpenTaiko.SaveFileInstances[p].data.Medals);

				#region [HiScore plate]

				var song = this.rNowSelectedSong;

				if (song != null && song.nodeType == CSongListNode.ENodeType.SCORE) {
					var closest = this.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song);
					var score = song.score[closest];

					if (score != null) {
						int displayedScore = 0;
						int table = 0;

						OpenTaiko.Tx.SongSelect_High_Score?.t2D中心基準描画(OpenTaiko.Skin.SongSelect_High_Score_X[i], OpenTaiko.Skin.SongSelect_High_Score_Y[i]);

						if (this.n現在選択中の曲の難易度 > (int)Difficulty.Edit)
							table = 0;
						else if (currentPads[i] <= (int)Difficulty.Edit)
							table = currentPads[i];
						else
							table = closest;

						var TableEntry = OpenTaiko.SaveFileInstances[p].data.tGetSongSelectTableEntry(OpenTaiko.stageSongSelect.rNowSelectedSong.tGetUniqueId());
						displayedScore = TableEntry.HighScore[table];

						if (this.n現在選択中の曲の難易度 <= (int)Difficulty.Edit) {
							CTexture __tex = (OpenTaiko.Tx.SongSelect_Difficulty_Cymbol == null) ? OpenTaiko.Tx.Dani_Difficulty_Cymbol : OpenTaiko.Tx.SongSelect_Difficulty_Cymbol;
							int width = __tex.sz画像サイズ.Width / 5;
							int height = __tex.sz画像サイズ.Height;

							__tex.t2D中心基準描画(
								OpenTaiko.Skin.SongSelect_High_Score_Difficulty_Cymbol_X[i],
								OpenTaiko.Skin.SongSelect_High_Score_Difficulty_Cymbol_Y[i],
								new Rectangle(table * width, 0, width, height));
						}

						tBoardNumberDraw(OpenTaiko.Skin.SongSelect_BoardNumber_X[i][12], OpenTaiko.Skin.SongSelect_BoardNumber_Y[i][12], displayedScore);
					}

				}

				#endregion
			}

			#endregion


			#region [ Inputs ]

			// キー入力
			if (base.ePhaseID == CStage.EPhase.Common_NORMAL) {
				#region [ 簡易CONFIGでMore、またはShift+F1: 詳細CONFIG呼び出し ]
				if (actQuickConfig.bGotoDetailConfig) {   // 詳細CONFIG呼び出し
					actQuickConfig.tDeativatePopupMenu();
					this.actPresound.tStopSound();
					this.eフェードアウト完了時の戻り値 = EReturnValue.ConfigMenuOpened;  // #24525 2011.3.16 yyagi: [SHIFT]-[F1]でCONFIG呼び出し
					this.actFIFO.tフェードアウト開始();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					OpenTaiko.Skin.soundCancelSFX.tPlay();
					return 0;
				}
				#endregion

				if (this.actSongList.isContextBoxOpened == true) {
					// Handle menu contexts
					bool __done = this.actSongList.tMenuContextController(this.actSongList.latestContext);
					if (__done == true) {
						if (this.actSongList.latestContext == eMenuContext.SearchByDifficulty) {
							#region [Trigger context box]

							this.actSongList.rCurrentlySelectedSong.childrenList = CSongDict.tFetchSongsByDifficulty(
								this.actSongList.rCurrentlySelectedSong,
								this.actSongList.tMenuContextGetVar(0),
								this.actSongList.tMenuContextGetVar(1));

							CSongSelectSongManager.disable();

							OpenTaiko.Skin.soundDecideSFX.tPlay();
							this.actSongList.ctBarFlash.Start(0, 2700, 1, OpenTaiko.Timer);
							this.actSongList.ctBoxOpen.Start(200, 2700, 1.3f, OpenTaiko.Timer);
							this.actSongList.bBoxOpen = true;

							//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
							CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);

							#endregion
						} else if (this.actSongList.latestContext == eMenuContext.SearchByText) {
							#region [Trigger context box]

							this.actSongList.rCurrentlySelectedSong.childrenList = CSongDict.tFetchSongsByTitle(
								this.actSongList.rCurrentlySelectedSong,
								(ETitleType)this.actSongList.tMenuContextGetVar(1),
								this.actSongList.searchTextResult);

							CSongSelectSongManager.disable();

							OpenTaiko.Skin.soundDecideSFX.tPlay();
							this.actSongList.ctBarFlash.Start(0, 2700, 1, OpenTaiko.Timer);
							this.actSongList.ctBoxOpen.Start(200, 2700, 1.3f, OpenTaiko.Timer);
							this.actSongList.bBoxOpen = true;

							CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
							#endregion
						} else if (this.actSongList.latestContext == eMenuContext.Random) {
							#region [Trigger context box]

							this.tSetSongRandomly();

							// Called here
							OpenTaiko.Skin.soundDecideSFX.tPlay();
							this.actDifficultySelectionScreen.bIsDifficltSelect = true;
							this.actDifficultySelectionScreen.t選択画面初期化();
							this.actSongList.ctBarFlash.Start(0, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);
							this.actSongList.ctDifficultyIn.Start(0, 3200, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);

							//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
							CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);

							#endregion
						}

						this.actSongList.tMenuContextDisable();
					}
				} else if (!this.actSortSongs.bIsActivePopupMenu && !this.actQuickConfig.bIsActivePopupMenu && !this.actDifficultySelectionScreen.bIsDifficltSelect && !actNewHeya.IsOpend) {
					#region [ ESC ]
					if ((OpenTaiko.Pad.bPressedDGB(EPad.Cancel) || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)) && (this.actSongList.rCurrentlySelectedSong != null))// && (  ) ) )
						if (this.actSongList.rCurrentlySelectedSong.rParentNode == null) {   // [ESC]
							this.actPresound.tStopSound();
							CSongSelectSongManager.enable();

							OpenTaiko.Skin.soundCancelSFX.tPlay();
							this.eフェードアウト完了時の戻り値 = EReturnValue.BackToTitle;
							this.actFIFO.tフェードアウト開始();
							base.ePhaseID = CStage.EPhase.Common_FADEOUT;
							return 0;
						} else {
							if (this.actSongList.ctBoxOpen.IsEnded || this.actSongList.ctBoxOpen.CurrentValue == 0) {
								this.actPresound.tStopSound();
								CSongSelectSongManager.enable();

								OpenTaiko.Skin.soundCancelSFX.tPlay();
								this.actSongList.ctBarFlash.Start(0, 2700, 1, OpenTaiko.Timer);
								this.actSongList.ctBoxOpen.Start(200, 2700, 1.3f, OpenTaiko.Timer);
								this.actSongList.bBoxClose = true;
								//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
								CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
							}
						}
					#endregion
					#region [ Shift-F1: Config shortcut ]
					if ((OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift) || OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift)) &&
						OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.F1)) {   // [SHIFT] + [F1] CONFIG
						this.actPresound.tStopSound();
						this.eフェードアウト完了時の戻り値 = EReturnValue.ConfigMenuOpened;  // #24525 2011.3.16 yyagi: [SHIFT]-[F1]でCONFIG呼び出し
						this.actFIFO.tフェードアウト開始();
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
						OpenTaiko.Skin.soundCancelSFX.tPlay();
						return 0;
					}
					#endregion
					#region [ F2 Quick Config ]
					if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.QuickConfig)) {
						OpenTaiko.Skin.soundChangeSFX.tPlay();
						this.actQuickConfig.tActivatePopupMenu(EInstrumentPad.Drums);
					}
					#endregion
					#region [ F3 1P AUTO ]
					if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.ToggleAutoP1)) {
						OpenTaiko.Skin.soundChangeSFX.tPlay();
						CUtility.ToggleBoolian(ref OpenTaiko.ConfigIni.bAutoPlay[0]);
					}
					#endregion
					#region [ F4 2P AUTO ]
					if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.ToggleAutoP2)) {
						if (OpenTaiko.ConfigIni.nPlayerCount > 1) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							CUtility.ToggleBoolian(ref OpenTaiko.ConfigIni.bAutoPlay[1]);
						}
					}
					#endregion
					#region [ F5 Super Hard Mode (DEPRECATED / UNUSED) ]
					if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.F5)) {
						// Deprecated, to delete
						OpenTaiko.Skin.soundChangeSFX.tPlay();
						CUtility.ToggleBoolian(ref OpenTaiko.ConfigIni.bSuperHard);
					}
					#endregion
					#region [ F7 TokkunMode ]
					if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.ToggleTrainingMode)) {
						if (OpenTaiko.ConfigIni.nPlayerCount < 2) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							CUtility.ToggleBoolian(ref OpenTaiko.ConfigIni.bTokkunMode);
						}
					}
					#endregion
					#region [ F9 New Heya ]
					if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.NewHeya)) {
						actNewHeya.Open();
					}
					#endregion

					if (this.actSongList.rCurrentlySelectedSong != null) {

						if (this.actSongList.ctBoxOpen.IsEnded || this.actSongList.ctBoxOpen.CurrentValue == 0) {
							if (!this.bCurrentlyScrolling) {
								#region [ Decide ]
								if ((OpenTaiko.Pad.bPressedDGB(EPad.Decide) ||
									 ((OpenTaiko.ConfigIni.bEnterIsNotUsedInKeyAssignments && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))))) {

									if (this.actSongList.rCurrentlySelectedSong != null) {
										switch (this.actSongList.rCurrentlySelectedSong.nodeType) {
											case CSongListNode.ENodeType.SCORE: {
													var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(this.rNowSelectedSong);

													if (IsSongLocked) {
														var SongToUnlock = OpenTaiko.Databases.DBSongUnlockables.tGetUnlockableByUniqueId(this.rNowSelectedSong);

														if (SongToUnlock != null) {
															(bool, string?) response = SongToUnlock.unlockConditions.tConditionMet(OpenTaiko.SaveFile, CUnlockCondition.EScreen.SongSelect);

															Color responseColor = (response.Item1) ? Color.Lime : Color.Red;
															if (actSongList.ttkNowUnlockConditionText is not null) {
																actSongList.ttkNowUnlockConditionText = new TitleTextureKey(
																	response.Item2 ?? actSongList.ttkNowUnlockConditionText.str,
																	actSongList.ttkNowUnlockConditionText.cPrivateFastFont,
																	responseColor, Color.Black, 1000);
															}

															if (response.Item1) {
																OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data.UnlockedSongs.Add(this.rNowSelectedSong?.tGetUniqueId() ?? "");
																DBSaves.RegisterStringUnlockedAsset(
																	OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data.SaveId,
																	"unlocked_songs",
																	this.rNowSelectedSong?.tGetUniqueId() ?? ""                     // Can't be null in this context
																);
																if (SongToUnlock.unlockConditions is CUnlockCM)
																	OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].tSpendCoins(SongToUnlock.unlockConditions.Values[0]);
																else if (SongToUnlock.unlockConditions is CUnlockAndComb || SongToUnlock.unlockConditions is CUnlockOrComb)
																	OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].tSpendCoins(SongToUnlock.unlockConditions.CoinStack);
																// Play modal animation here ?
															} else
																OpenTaiko.Skin.soundError.tPlay();
														} else {
															OpenTaiko.Skin.soundError.tPlay();
														}
													} else {
														if (this.n現在選択中の曲の難易度 >= (int)Difficulty.Tower) {
															if (OpenTaiko.ConfigIni.nPlayerCount == 1 && !OpenTaiko.ConfigIni.bTokkunMode) {
																// Init tower variables
																if (this.n現在選択中の曲の難易度 == (int)Difficulty.Tower)
																	CFloorManagement.reinitialize(this.rNowSelectedSong.score[(int)Difficulty.Tower].譜面情報.nLife);

																OpenTaiko.Skin.soundDecideSFX.tPlay();
																OpenTaiko.Skin.voiceMenuSongDecide[OpenTaiko.SaveFile]?.tPlay();

																this.t曲を選択する();
															} else {
																OpenTaiko.Skin.soundError.tPlay();
															}
														} else {
															// Called here
															OpenTaiko.Skin.soundDecideSFX.tPlay();
															this.actDifficultySelectionScreen.bIsDifficltSelect = true;
															this.actDifficultySelectionScreen.t選択画面初期化();
															this.actSongList.ctBarFlash.Start(0, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);
															this.actSongList.ctDifficultyIn.Start(0, 3200, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);
															//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
															CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
														}
													}
												}
												break;
											case CSongListNode.ENodeType.BOX: {

													#region [Pre-generated folders]

													if (this.actSongList.rCurrentlySelectedSong.songGenre == "Favorite") {
														this.actSongList.rCurrentlySelectedSong.childrenList = CSongDict.tFetchFavoriteFolder(this.actSongList.rCurrentlySelectedSong);
													} else if (this.actSongList.rCurrentlySelectedSong.songGenre == "最近遊んだ曲") {
														this.actSongList.rCurrentlySelectedSong.childrenList = CSongDict.tFetchRecentlyPlayedSongsFolder(this.actSongList.rCurrentlySelectedSong);
													} else if (this.actSongList.rCurrentlySelectedSong.songGenre == "SearchD") {
														this.actSongList.tMenuContextTrigger(eMenuContext.SearchByDifficulty);
														OpenTaiko.Skin.soundDecideSFX.tPlay();
														goto Decided;
														//this.act曲リスト.r現在選択中の曲.list子リスト = CSongDict.tFetchSongsByDifficulty(this.act曲リスト.r現在選択中の曲, (int)Difficulty.Oni, 8);
													} else if (this.actSongList.rCurrentlySelectedSong.songGenre == "SearchT") {
														this.actSongList.tMenuContextTrigger(eMenuContext.SearchByText);
														OpenTaiko.Skin.soundDecideSFX.tPlay();
														goto Decided;
													}

													#endregion

													CSongSelectSongManager.disable();

													OpenTaiko.Skin.soundDecideSFX.tPlay();
													this.actSongList.ctBarFlash.Start(0, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);
													this.actSongList.ctBoxOpen.Start(200, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval * 1.3f, OpenTaiko.Timer);
													this.actSongList.bBoxOpen = true;
													//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
													CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
												}
												break;
											case CSongListNode.ENodeType.BACKBOX: {
													// TOJIRU
													CSongSelectSongManager.enable();

													OpenTaiko.Skin.soundCancelSFX.tPlay();
													this.actSongList.ctBarFlash.Start(0, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);
													this.actSongList.ctBoxOpen.Start(200, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval * 1.3f, OpenTaiko.Timer);
													this.actSongList.bBoxClose = true;
													//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
													CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
												}
												break;
											case CSongListNode.ENodeType.RANDOM: {
													this.tSetSongRandomly();

													// Called here
													OpenTaiko.Skin.soundDecideSFX.tPlay();
													this.actDifficultySelectionScreen.bIsDifficltSelect = true;
													this.actDifficultySelectionScreen.t選択画面初期化();
													this.actSongList.ctBarFlash.Start(0, 2700, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);
													this.actSongList.ctDifficultyIn.Start(0, 3200, OpenTaiko.Skin.SongSelect_Box_Opening_Interval, OpenTaiko.Timer);

													//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
													CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);

													/*
													TJAPlayer3.Skin.sound決定音.t再生する();

													this.act曲リスト.tMenuContextTrigger(eMenuContext.Random);
													goto Decided;
													*/

													/*
													this.t曲をランダム選択する();

													//this.ctChara_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
													CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
													*/
												}
												break;
										}

										goto Decided;

									}
								}
								#endregion

							}

							#region [ Favorite ]

							if (!this.bCurrentlyScrolling) {
								var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(this.rNowSelectedSong);

								if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftControl) && !IsSongLocked) {
									CSongUniqueID csu = this.rNowSelectedSong.uniqueId;

									if (csu != null) {
										OpenTaiko.Skin.soundDecideSFX.tPlay();
										OpenTaiko.Favorites.tToggleFavorite(csu.data.id);
									}
								}
							}

							#endregion

							#region [ Up ]
							if (!this.bCurrentlyScrolling) {
								this.ctキー反復用.Up.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftArrow), new CCounter.KeyProcess(this.tカーソルを上へ移動する));
								//this.ctキー反復用.Up.tキー反復( CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.UpArrow ) || CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.LeftArrow ), new CCounter.DGキー処理( this.tカーソルを上へ移動する ) );
								if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LeftChange)) {
									this.tカーソルを上へ移動する();
								}
							} else {
								if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LeftChange)) {
									//this.ctChara_Jump[0].t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
									//this.ctChara_Jump[1].t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
									CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);

									for (int i = 0; i < 7; i++) tカーソルスキップ(true);
								}
							}
							#endregion

							#region [ Down ]
							if (!this.bCurrentlyScrolling) {
								this.ctキー反復用.Down.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightArrow), new CCounter.KeyProcess(this.tカーソルを下へ移動する));
								//this.ctキー反復用.Down.tキー反復( CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.DownArrow ) || CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.RightArrow ), new CCounter.DGキー処理( this.tカーソルを下へ移動する ) );

								if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RightChange)) {
									this.tカーソルを下へ移動する();
								}
							} else {
								if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RightChange)) {
									//this.ctChara_Jump[0].t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
									//this.ctChara_Jump[1].t開始(0, TJAPlayer3.Tx.SongSelect_Chara_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
									CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);

									for (int i = 0; i < 7; i++) tカーソルスキップ(false);
								}
							}
						#endregion

						Decided:;

						}
						#region [ Upstairs ]
						/*
                        if (((this.actSongList.rCurrentlySelectedSong != null) && (this.actSongList.rCurrentlySelectedSong.rParentNode != null)) && (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.FT) || TJAPlayer3.Pad.bPressedGB(EPad.Cancel)))
                        {
                            this.actPresound.tStopSound();
                            TJAPlayer3.Skin.soundCancelSFX.tPlay();
                            this.actSongList.tCloseBOX();
                            this.tNotifySelectedSongChange();
                        }
                        */
						#endregion
						#region [ BDx2: 簡易CONFIG ]
						if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.SortSongs)) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							this.actSortSongs.tActivatePopupMenu(EInstrumentPad.Drums, ref this.actSongList);
						}
						#endregion
						#region [ HHx2: 難易度変更 ]
						if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.HH) || OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.HHO)) {   // [HH]x2 難易度変更
							CommandHistory.Add(EInstrumentPad.Drums, EPadFlag.HH);
							EPadFlag[] comChangeDifficulty = new EPadFlag[] { EPadFlag.HH, EPadFlag.HH };
							if (CommandHistory.CheckCommand(comChangeDifficulty, EInstrumentPad.Drums)) {
								Debug.WriteLine("ドラムス難易度変更");
								this.actSongList.t難易度レベルをひとつ進める();
								OpenTaiko.Skin.soundChangeSFX.tPlay();
							}
						}
						#endregion
					}
				}




				#region [ Minus & Equals Sound Group Level ]
				KeyboardSoundGroupLevelControlHandler.Handle(
					OpenTaiko.InputManager.Keyboard, OpenTaiko.SoundGroupLevelController, OpenTaiko.Skin, true);
				#endregion

				this.actSortSongs.Draw();
				this.actQuickConfig.Draw();
			}

			#endregion

			//------------------------------
			if (this.actDifficultySelectionScreen.bIsDifficltSelect) {
				if (this.actSongList.ctDifficultyIn.CurrentValue >= 1255) {
					this.actDifficultySelectionScreen.Draw();
				}
			}
			//------------------------------


			if (OpenTaiko.ConfigIni.nPlayerCount == 1) {
				var opacity = 0;

				if (ctCreditAnime.CurrentValue <= 510)
					opacity = ctCreditAnime.CurrentValue / 2;
				else if (ctCreditAnime.CurrentValue <= 4500 - 510)
					opacity = 255;
				else
					opacity = 255 - ((ctCreditAnime.CurrentValue - (4500 - 510)) / 2);

				OpenTaiko.Tx.SongSelect_Credit.Opacity = opacity;

				OpenTaiko.Tx.SongSelect_Credit.t2D描画(0, 0);
			}

			for (int i = 0; i < 5; i++) {
				CCounter ___cs = CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.START)[i];

				//if (this.ctChara_Jump[i].n現在の値 >= this.ctChara_Jump[i].n終了値)
				if (___cs != null && ___cs.CurrentValue >= ___cs.EndValue) {
					//this.ctChara_Jump[i].t停止();
					___cs.Stop();

					if (!this.actDifficultySelectionScreen.bIsDifficltSelect) {
						//this.ctChara_Jump[i].n現在の値 = 0;
						___cs.CurrentValue = 0;
					}
				}
			}

			this.actPlayOption.On進行描画(5, this.actDifficultySelectionScreen.bOption);

			if (actNewHeya.IsOpend) actNewHeya.Draw();

			switch (base.ePhaseID) {
				case CStage.EPhase.Common_FADEIN:
					if (this.actFIFO.Draw() != 0) {
						base.ePhaseID = CStage.EPhase.Common_NORMAL;
					}
					break;

				case CStage.EPhase.Common_FADEOUT:
					if (this.actFIFO.Draw() == 0) {
						break;
					}
					return (int)this.eフェードアウト完了時の戻り値;

				case CStage.EPhase.SongSelect_FadeInFromResults:
					if (this.actFIfrom結果画面.Draw() != 0) {
						base.ePhaseID = CStage.EPhase.Common_NORMAL;
					}
					break;

				case CStage.EPhase.SongSelect_FadeOutToNowLoading:
					if (this.actFOtoNowLoading.Draw() == 0) {
						break;
					}
					return (int)this.eフェードアウト完了時の戻り値;
			}
		}
		return 0;
	}
	public enum EReturnValue : int {
		継続,
		BackToTitle,
		PlayCutSceneIntro,
		SongSelected,
		オプション呼び出し,
		ConfigMenuOpened,
		SkinChange
	}


	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STキー反復用カウンタ {
		public CCounter Up;
		public CCounter Down;
		public CCounter this[int index] {
			get {
				switch (index) {
					case 0:
						return this.Up;

					case 1:
						return this.Down;
				}
				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case 0:
						this.Up = value;
						return;

					case 1:
						this.Down = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}
	private CCounter ctTimer;
	private CCounter ctCreditAnime;
	private Random[] r = new Random[3];
	public CCounter ctBackgroundFade;
	public string NowGenre;
	public string OldGenre;
	public string NowBg;
	public string OldBg;
	public Color NowBgColor = Color.White;
	public Color OldBgColor = Color.White;
	public bool NowUseGenre;
	public bool OldUseGenre;
	private CActSelectArtistComment actArtistComment;
	private CActFIFOBlack actFIFO;
	private CActFIFOBlack actFIfrom結果画面;
	//private CActFIFOBlack actFOtoNowLoading;
	public CActFIFOStart actFOtoNowLoading;
	private CActSelectInformation actInformation;
	public CActSelectPreimageパネル actPreimageパネル;
	public CActSelectPresound actPresound;
	private CActオプションパネル actオプションパネル;
	private CActSelectステータスパネル actステータスパネル;
	public CActSelect演奏履歴パネル act演奏履歴パネル;
	public CActSelect曲リスト actSongList;
	public CActSelectSongInfo actSongInfo;
	public CActSelectDanInfo actDanInfo;
	public CActSelectTowerInfo actTowerInfo;
	private CActSelectShowCurrentPosition actShowCurrentPosition;
	public CActSelect難易度選択画面 actDifficultySelectionScreen;
	public CActPlayOption actPlayOption;
	public CActSelectExExtraTransAnime actExExtraTransAnime;
	public CActNewHeya actNewHeya;

	public CActSortSongs actSortSongs;
	private CActSelectQuickConfig actQuickConfig;

	private ScriptBG AI_Background;

	private const int MaxSong = 3;
	public int NowSong = 1;

	//private CCounter ctChara_Normal;
	//private CCounter ctChara_Select;
	//public CCounter[] ctChara_Jump = new CCounter[2];

	public PuchiChara PuchiChara;

	private string nGenreBack;
	private string nOldGenreBack;
	public bool bBGM再生済み;
	public bool bBGMIn再生した;
	private STキー反復用カウンタ ctキー反復用;
	public CCounter ct登場時アニメ用共通;
	private CCounter ctOldBGScroll;
	private CCounter ct背景スクロール用タイマー;
	private EReturnValue eフェードアウト完了時の戻り値;
	private CCachedFontRenderer ftフォント;
	//private CTexture tx下部パネル;
	//private CTexture tx上部パネル;
	//private CTexture tx背景;
	//      private CTexture[] txジャンル別背景 = new CTexture[9];
	//      private CTexture[] tx難易度別背景 = new CTexture[5];
	//      private CTexture tx難易度名;
	//      private CTexture tx下部テキスト;
	private CCounter ctDiffSelect移動待ち;

	private STNumber[] stSongNumber = new STNumber[10];
	private STNumber[] stBoardNumber = new STNumber[10];

	public struct STNumber {
		public char ch;
		public Point pt;
	}
	private struct STCommandTime        // #24063 2011.1.16 yyagi コマンド入力時刻の記録用
	{
		public EInstrumentPad eInst;        // 使用楽器
		public EPadFlag ePad;       // 押されたコマンド(同時押しはOR演算で列挙する)
		public long time;               // コマンド入力時刻
	}

	/*
    private Point[] ptBoardNumber =
        { new Point(72, 283), new Point(135, 283), new Point(200, 283), new Point(72, 258), new Point(135, 258), new Point(200, 258), new Point(200, 233), new Point(72, 311), new Point(135, 311), new Point(200, 311), new Point(84, 360), new Point(124, 416) };
    */

	private CTexture txCustomSelectBG;
	private CTexture txCustomPrevSelectBG;
	private CTexture txGenreBack {
		get {
			if (txCustomSelectBG == null) {
				return HGenreBar.tGetGenreBar(nGenreBack, OpenTaiko.Tx.SongSelect_GenreBack);
			} else {
				return txCustomSelectBG;
			}
		}
	}
	private CTexture txOldGenreBack {
		get {
			if (txCustomPrevSelectBG == null) {
				return HGenreBar.tGetGenreBar(nOldGenreBack, OpenTaiko.Tx.SongSelect_GenreBack);
			} else {
				return txCustomPrevSelectBG;
			}
		}
	}

	public void tBoardNumberDraw(int x, int y, int num) {
		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - (nums.Length / 2.0f);
			float _x = x - (OpenTaiko.Skin.SongSelect_BoardNumber_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.SongSelect_BoardNumber_Interval[1] * offset);

			float width = OpenTaiko.Tx.SongSelect_BoardNumber.sz画像サイズ.Width / 10.0f;
			float height = OpenTaiko.Tx.SongSelect_BoardNumber.sz画像サイズ.Height;

			OpenTaiko.Tx.SongSelect_BoardNumber.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tSongNumberDraw(int x, int y, int num) {
		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - (nums.Length / 2.0f);
			float _x = x - (OpenTaiko.Skin.SongSelect_SongNumber_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.SongSelect_SongNumber_Interval[1] * offset);

			float width = OpenTaiko.Tx.SongSelect_Song_Number.sz画像サイズ.Width / 10.0f;
			float height = OpenTaiko.Tx.SongSelect_Song_Number.sz画像サイズ.Height;

			OpenTaiko.Tx.SongSelect_Song_Number.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tTimerDraw(int num) {
		//int x = 1171, y = 57;

		int[] nums = CConversion.SeparateDigits(num);

		for (int j = 0; j < nums.Length; j++) {
			if (OpenTaiko.ConfigIni.bEnableCountdownTimer) {
				float offset = j - (nums.Length / 2.0f);
				float x = OpenTaiko.Skin.SongSelect_Timer[0] - (int)(OpenTaiko.Skin.SongSelect_Timer_Interval[0] * offset);
				float y = OpenTaiko.Skin.SongSelect_Timer[1] - (int)(OpenTaiko.Skin.SongSelect_Timer_Interval[1] * offset);

				float width = OpenTaiko.Tx.SongSelect_Timer.sz画像サイズ.Width / 10.0f;
				float height = OpenTaiko.Tx.SongSelect_Timer.sz画像サイズ.Height / 2.0f;

				OpenTaiko.Tx.SongSelect_Timer.t2D描画(x, y, new RectangleF(width * nums[j], 0, width, height));
			}
		}
	}



	private class CCommandHistory       // #24063 2011.1.16 yyagi コマンド入力履歴を保持_確認するクラス
	{
		readonly int buffersize = 16;
		private List<STCommandTime> stct;

		public CCommandHistory()        // コンストラクタ
		{
			stct = new List<STCommandTime>(buffersize);
		}

		/// <summary>
		/// コマンド入力履歴へのコマンド追加
		/// </summary>
		/// <param name="_eInst">楽器の種類</param>
		/// <param name="_ePad">入力コマンド(同時押しはOR演算で列挙すること)</param>
		public void Add(EInstrumentPad _eInst, EPadFlag _ePad) {
			STCommandTime _stct = new STCommandTime {
				eInst = _eInst,
				ePad = _ePad,
				time = OpenTaiko.Timer.NowTimeMs
			};

			if (stct.Count >= buffersize) {
				stct.RemoveAt(0);
			}
			stct.Add(_stct);
			//Debug.WriteLine( "CMDHIS: 楽器=" + _stct.eInst + ", CMD=" + _stct.ePad + ", time=" + _stct.time );
		}
		public void RemoveAt(int index) {
			stct.RemoveAt(index);
		}

		/// <summary>
		/// コマンド入力に成功しているか調べる
		/// </summary>
		/// <param name="_ePad">入力が成功したか調べたいコマンド</param>
		/// <param name="_eInst">対象楽器</param>
		/// <returns>コマンド入力成功時true</returns>
		public bool CheckCommand(EPadFlag[] _ePad, EInstrumentPad _eInst) {
			int targetCount = _ePad.Length;
			int stciCount = stct.Count;
			if (stciCount < targetCount) {
				//Debug.WriteLine("NOT start checking...stciCount=" + stciCount + ", targetCount=" + targetCount);
				return false;
			}

			long curTime = OpenTaiko.Timer.NowTimeMs;
			//Debug.WriteLine("Start checking...targetCount=" + targetCount);
			for (int i = targetCount - 1, j = stciCount - 1; i >= 0; i--, j--) {
				if (_ePad[i] != stct[j].ePad) {
					//Debug.WriteLine( "CMD解析: false targetCount=" + targetCount + ", i=" + i + ", j=" + j + ": ePad[]=" + _ePad[i] + ", stci[j] = " + stct[j].ePad );
					return false;
				}
				if (stct[j].eInst != _eInst) {
					//Debug.WriteLine( "CMD解析: false " + i );
					return false;
				}
				if (curTime - stct[j].time > 500) {
					//Debug.WriteLine( "CMD解析: false " + i + "; over 500ms" );
					return false;
				}
				curTime = stct[j].time;
			}

			//Debug.Write( "CMD解析: 成功!(" + _ePad.Length + ") " );
			//for ( int i = 0; i < _ePad.Length; i++ ) Debug.Write( _ePad[ i ] + ", " );
			//Debug.WriteLine( "" );
			//stct.RemoveRange( 0, targetCount );			// #24396 2011.2.13 yyagi
			stct.Clear();                                   // #24396 2011.2.13 yyagi Clear all command input history in case you succeeded inputting some command

			return true;
		}
	}
	private CCommandHistory CommandHistory;

	private void tカーソルを下へ移動する() {
		if ((this.actSongList.rGetSideSong(1).nodeType == CSongListNode.ENodeType.SCORE) || this.actSongList.rGetSideSong(1).nodeType == CSongListNode.ENodeType.BACKBOX) {
			OpenTaiko.stageSongSelect.bBGMIn再生した = false;

			CSongSelectSongManager.disable();
		} else {
			CSongSelectSongManager.enable();
			CSongSelectSongManager.playSongIfPossible();
		}
		this.ctBackgroundFade.Start(0, 600, 1, OpenTaiko.Timer);
		if (this.actSongList.ctBarOpen.CurrentValue >= 200 || this.ctBackgroundFade.CurrentValue >= 600 - 255) {
			OpenTaiko.stageSongSelect.OldGenre = this.rNowSelectedSong.songGenre;
			OpenTaiko.stageSongSelect.OldUseGenre = !this.rNowSelectedSong.isChangedBgType;
			OpenTaiko.stageSongSelect.OldBg = this.rNowSelectedSong.BgType;
			OpenTaiko.stageSongSelect.OldBgColor = this.rNowSelectedSong.BgColor;
		}

		this.actSongList.t次に移動();
		OpenTaiko.Skin.soundカーソル移動音.tPlay();
	}
	private void tカーソルを上へ移動する() {
		if ((this.actSongList.rGetSideSong(-1).nodeType == CSongListNode.ENodeType.SCORE) || this.actSongList.rGetSideSong(-1).nodeType == CSongListNode.ENodeType.BACKBOX) {
			OpenTaiko.stageSongSelect.bBGMIn再生した = false;

			CSongSelectSongManager.disable();
		} else {
			CSongSelectSongManager.enable();
			CSongSelectSongManager.playSongIfPossible();
		}

		this.ctBackgroundFade.Start(0, 600, 1, OpenTaiko.Timer);
		if (this.actSongList.ctBarOpen.CurrentValue >= 200 || this.ctBackgroundFade.CurrentValue >= 600 - 255) {
			OpenTaiko.stageSongSelect.OldGenre = this.rNowSelectedSong.songGenre;
			OpenTaiko.stageSongSelect.OldUseGenre = !this.rNowSelectedSong.isChangedBgType;
			OpenTaiko.stageSongSelect.OldBg = this.rNowSelectedSong.BgType;
			OpenTaiko.stageSongSelect.OldBgColor = this.rNowSelectedSong.BgColor;
		}

		this.actSongList.t前に移動();
		OpenTaiko.Skin.soundカーソル移動音.tPlay();
	}
	private void tカーソルスキップ(bool Up) {
		this.ctBackgroundFade.Start(0, 600, 1, OpenTaiko.Timer);
		if (this.actSongList.ctBarOpen.CurrentValue >= 200 || this.ctBackgroundFade.CurrentValue >= 600 - 255) {
			OpenTaiko.stageSongSelect.OldGenre = this.rNowSelectedSong.songGenre;
			OpenTaiko.stageSongSelect.OldUseGenre = !this.rNowSelectedSong.isChangedBgType;
			OpenTaiko.stageSongSelect.OldBg = this.rNowSelectedSong.BgType;
			OpenTaiko.stageSongSelect.OldBgColor = this.rNowSelectedSong.BgColor;
		}

		if (Up) this.actSongList.t前に移動();
		else this.actSongList.t次に移動();

		OpenTaiko.Skin.soundSkip.tPlay();
	}

	private int tGetRandomSongDifficulty(int contextDiff) {
		var song = this.rChoosenSong;

		int baseDiff = this.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.rChoosenSong);

		if (contextDiff >= 0) {
			if (contextDiff < (int)Difficulty.Oni)
				return contextDiff;
			else if (contextDiff == (int)Difficulty.Oni) {
				var score = song.score[baseDiff];
				if (score.譜面情報.nレベル[(int)Difficulty.Oni] >= 0 && score.譜面情報.nレベル[(int)Difficulty.Edit] >= 0)
					return (new Random().Next(0, 2) == 0 ? (int)Difficulty.Oni : (int)Difficulty.Edit);
				return (int)Difficulty.Oni;
			}
		}

		return baseDiff;
	}

	private void tSetSongRandomly() {
		var usedDiffs = new int[] { -1, -1, -1, -1, -1 };
		var mandatoryDiffs = new List<int>();

		#region [Fetch context informations]

		if (this.actSongList.latestContext == eMenuContext.Random) {
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				var diff = this.actSongList.tMenuContextGetVar(i);
				usedDiffs[i] = diff;
				if (!mandatoryDiffs.Contains(diff))
					mandatoryDiffs.Add(diff);
			}
		}

		#endregion

		CSongListNode song = this.actSongList.rCurrentlySelectedSong;

		song.randomList = this.t指定された曲が存在する場所の曲を列挙する_子リスト含む(song, ref mandatoryDiffs);
		int selectableSongCount = song.randomList.Count;

		if (selectableSongCount == 0) {
			return;
		}

		int randomSongIndex = OpenTaiko.Random.Next(selectableSongCount);

		if (OpenTaiko.ConfigIni.bOutputDetailedDTXLog) {
			StringBuilder builder = new StringBuilder(0x400);
			builder.Append(string.Format("Total number of songs to randomly choose from {0}. Randomly selected index {0}.", selectableSongCount, randomSongIndex));
			Trace.TraceInformation(builder.ToString());
		}

		// Third assignment
		this.rNowSelectedSong = song.randomList[randomSongIndex];

		actSongList.t現在選択中の曲を元に曲バーを再構成する();
		actSongList.t選択曲が変更された(false);
		actSongList.tUpdateCurSong();
		actSongList.tResetTitleKey();
		actSongList.tバーの初期化();
		tNotifySelectedSongChange();
	}
	private void t曲を選択する() {
		this.t曲を選択する(this.actSongList.n現在選択中の曲の現在の難易度レベル, 0);
	}
	public void t曲を選択する(int nCurrentLevel, int player) {
		this.rChoosenSong = this.actSongList.rCurrentlySelectedSong;
		this.r確定されたスコア = this.actSongList.r現在選択中のスコア;

		this.nChoosenSongDifficulty[player] = nCurrentLevel;
		this.str確定された曲のジャンル = this.rChoosenSong.songGenre;

		if ((this.rChoosenSong != null) && (this.r確定されたスコア != null)) {
			if (OpenTaiko.stageCutScene.LoadCutScenes(this)) {
				this.FadeToCutSceneIntro();
			} else {
				this.FadeOutToNowLoading();
			}
		}

		// TJAPlayer3.Skin.bgm選曲画面.t停止する();
		CSongSelectSongManager.stopSong();
	}

	private void FadeToCutSceneIntro() {
		this.eフェードアウト完了時の戻り値 = EReturnValue.PlayCutSceneIntro;
		this.actFIFO.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
		base.ePhaseID = CStage.EPhase.Common_FADEOUT;
	}

	private void FadeOutToNowLoading() {
		this.eフェードアウト完了時の戻り値 = EReturnValue.SongSelected;
		this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
		base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;
	}

	// Foreach randomly selectable songs
	private List<CSongListNode> t指定された曲が存在する場所の曲を列挙する_子リスト含む(CSongListNode song, ref List<int> mandatory) {
		List<CSongListNode> list = new List<CSongListNode>();
		song = song.rParentNode;
		if ((song == null) && (OpenTaiko.Songs管理.list曲ルート.Count > 0)) {
			foreach (CSongListNode c曲リストノード in OpenTaiko.Songs管理.list曲ルート) {
				if ((c曲リストノード.nodeType == CSongListNode.ENodeType.SCORE) || (c曲リストノード.nodeType == CSongListNode.ENodeType.SCORE_MIDI)) {
					// Don't add Dan/Tower charts for Random
					int diff = this.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(c曲リストノード);
					if (diff < (int)Difficulty.Tower) {
						// Check if mandatory diffs are present
						var score = c曲リストノード.score[diff];
						bool requiredDiffsExist = true;
						foreach (int df in mandatory) {
							if (score.譜面情報.nレベル[df] < 0) {
								requiredDiffsExist = false;
								break;
							}
						}

						var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(c曲リストノード);
						if (requiredDiffsExist == true && IsSongLocked == false) {
							list.Add(c曲リストノード);
						}

					}

				}
				if ((c曲リストノード.childrenList != null) && OpenTaiko.ConfigIni.bIncludeSubfoldersOnRandomSelect) {
					this.t指定された曲の子リストの曲を列挙する_孫リスト含む(c曲リストノード, ref list, ref mandatory);
				}
			}
			return list;
		}
		this.t指定された曲の子リストの曲を列挙する_孫リスト含む(song, ref list, ref mandatory);
		return list;
	}

	public void t指定された曲の子リストの曲を列挙する_孫リスト含む(CSongListNode r親, ref List<CSongListNode> list, ref List<int> mandatory, bool dan = false, Difficulty difficulty = Difficulty.Dan) {
		if ((r親 != null) && (r親.childrenList != null)) {
			foreach (CSongListNode c曲リストノード in r親.childrenList) {
				if ((c曲リストノード.nodeType == CSongListNode.ENodeType.SCORE) || (c曲リストノード.nodeType == CSongListNode.ENodeType.SCORE_MIDI)) {
					// Don't add Dan/Tower charts for Random
					int diff = this.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(c曲リストノード);

					if (dan ? diff == (int)difficulty : diff < (int)Difficulty.Tower) {
						// Check if mandatory diffs are present
						var score = c曲リストノード.score[diff];
						bool requiredDiffsExist = true;
						foreach (int df in mandatory) {
							if (score.譜面情報.nレベル[df] < 0) {
								requiredDiffsExist = false;
								break;
							}
						}

						var IsSongLocked = OpenTaiko.Databases.DBSongUnlockables.tIsSongLocked(c曲リストノード);
						if (requiredDiffsExist == true && IsSongLocked == false) {
							list.Add(c曲リストノード);
						}

					}
				}
				if ((c曲リストノード.childrenList != null) && OpenTaiko.ConfigIni.bIncludeSubfoldersOnRandomSelect) {
					this.t指定された曲の子リストの曲を列挙する_孫リスト含む(c曲リストノード, ref list, ref mandatory, dan, difficulty);
				}
			}
		}
	}

	//-----------------
	#endregion
}
