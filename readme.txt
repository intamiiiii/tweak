Std.Tweak Library

License:MIT/X11 License

お好きにどうぞ。
ただ、ちゃんと動かない可能性がアリアリです。アリアリ。

.NET Framework 4.0以降でご利用ください。

* 基本的な使い方
Std.Tweak.CredentialProviders.OAuthを使います。
　BASIC認証は今はtwitterがサポートしていないのでもはや限界です。
　xAuth認証を利用するにはtwitterにメールする必要があります。
始めに、OAuthを継承し、トークンとシークレットを適切に設定してください。
　OAuthは抽象クラスとなっているため、実体化できません。
GetProviderAuthUrlを実行して、リクエストトークンとアクセス先を取得、
URLにアクセスしてPINを取得、あとはGetAccessTokenでアクセストークンにして完成です。

Twitter APIを使うには
using Std.Tweak;
しておいて、拡張メソッドを使えるようにしておくと色々とはかどります。

OAuth cp = new DerivedOAuth();
～認証～
IEnumerable<TwitterStatus> tl = cp.GetHomeTimeline();

で、TwitterStatusのIEnumerableを取得できます。
DMはTwitterDirectMessageという別のクラスで表現されますが、
どちらもTwitterStatusBaseを継承しているので、両方まとめて管理したいときは
IEnumerable<TwitterStatusBase>とか使うと良いでしょう。

StreamingAPIは、Std.Tweak.Streaming.StreamingControllerを使います。
　Std.Tweak.Streaming.StreamingAPIは互換性のためにあるだけで、利用は推奨していません。obsoleteです。

先程のcpに対して、UserStreams接続を行うには

StreamingController s = StreamingController.BeginStreaming(
							cp, StreamingController.StreamingType.user);

と記述します。接続に成功した場合、s.EnumerateStreaming()を実行することでIEnumerable<TwitterStreamingElement>を取得できます。
無限列挙となるので、別スレッドでイテレートするか、もしくはデッドロックしないようにうまいことやる必要があります。

var action = new Action(()=>
{
	foreach(var e in s.EnumerateStreaming())
	{
		System.Diagnostics.Debug.WriteLine(e.ToString());
	}
}
action.BeginInvoke((iar)=>action.EndInvoke(iar), null);

とかね。

基本的にWebExceptionを吐きますが、IOExceptionやArgumentExceptionなども吐く可能性があります。
TwitterAPI自体が＜お察しください＞なので、まぁ、うん。
きちんとtry～catchしてください。

あとは感覚でどうにかしてくださいね。
こんなアレゲなライブラリ使う人もいないと思うけど。。。
