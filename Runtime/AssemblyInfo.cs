// Assembly info for Edanoue.SaveData.dll
// see: https://scrapbox.io/edanoue/AssemblyInfo.cs

using System.Runtime.CompilerServices;

// -------------------------
// Define Friend Assembries
// -------------------------

#if UNITY_EDITOR

// 以下の2つは本番環境で公開されないため, #if を置いておく

// Editor 用のアセンブリを Friend Assembly に指定する
[assembly: InternalsVisibleTo("Edanoue.SaveData.Editor")]

// Editor Test 用のアセンブリを Friend Assembly に指定する
[assembly: InternalsVisibleTo("Edanoue.SaveData.Editor.Tests")]

#endif
