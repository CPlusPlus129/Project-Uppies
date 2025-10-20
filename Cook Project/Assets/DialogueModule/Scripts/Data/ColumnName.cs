namespace DialogueModule
{
    public enum ColumnName
    {
        Tag,            //タグ
        Param1,         //パラメーター1
        Param2,         //パラメーター2
        CharacterName,  //キャラ名
        Pattern,        //パターン
        Pivot,          //ピボット位置
        Scale,          //スケール
        FileName,       //ファイル名
        Streaming,      //ストリーミング
        Version,        //ファイルバージョン
        LayerName,      //レイヤー名
        Type,           //タイプ
        X,              //座標X
        Y,              //座標Y
        Z,                  //Z値
        Order,          //描画順
        Label,          //ラベル
        Value,          //値

        Command,        //コマンド
        Arg1,           //引数1
        Arg2,           //引数2
        Arg3,           //引数3
        Arg4,           //引数4
        Arg5,           //引数5
        Arg6,           //引数6

        WindowType,     //メッセージウィンドウのタイプ
        PageCtrl,       //ページコントローラー
        Text,           //テキスト
        Voice,          //ボイス
        VoiceVersion,   //ボイスバージョン

        LayerMask,      //レイヤーマスク名（Unityのレイヤー名）
        Title,          //表示タイトル
        Thumbnail,      //サムネイル用ファイルのパス
        ThumbnailVersion,   //サムネイル用ファイルのバージョン

        ScenarioLabel,      //シナリオラベル
        NameText,           //表示名
        CgCategolly,        //CGのカテゴリ
        FileType,           //ファイルのタイプ

        Categolly,          //カテゴリ
        IgnoreLoad,         //ロード無視
        Conditional,        //条件式

        Chapter,            //チャプター
        RenderTexture,      //テクスチャ書き込みタイプ
        RenderRect,         //テクスチャ書き込みのテクスチャ矩形
        RenderTextureScale, //テクスチャ書き込みのスケール値

        Animation,          //アンメーション名
        EyeBlink,           //目パチ
        LipSynch,           //口パク
        Interval,           //インターバル時間
        IntervalMin,        //インターバル時間最小値
        IntervalMax,        //インターバル時間最大値
        Duration,           //期間（秒数）
        Name0,              //ミニアニメーションの名前のトップ
        Duration0,          //期間（秒数）のトップ
        ScaleVoiceVolume,   //ボイスボリュームのスケール値
        RandomDouble,       //二連続する確率

        ScaleX,             //スケールX
        ScaleY,             //スケールY
        ScaleZ,             //スケールZ

        Width,              //幅
        Height,             //高さ

        BorderLeft,         //余白値　左
        BorderRight,        //余白値　右
        BorderTop,          //余白値　上
        BorderBottom,       //余白値　下

        PivotX,             //ピボットX
        PivotY,             //ピボットY
        AnchorX,            //アンカーX
        AnchorY,            //アンカーY

        Align,              //配置
        FlipX,              //左右反転
        FlipY,              //上下反転		

        IntroTime,          //イントロループ用のループポイント
        Volume,             //ボリューム

        WaitType,           //待機のタイプ

        SubFileName,        //ダイシングなどのサブファイル名

        AnimationState,     //アニメーションステートの状態名を取得

        Icon,               //アイコンファイル
        IconRect,           //アイコンの矩形
        IconSubFileName,    //アイコンのサブファイル名
        IconAutoFlip,       //アイコンを自動的に反転させるか

        Pivot0,				//ピボット位置0(親オブジェクト)
    }
}