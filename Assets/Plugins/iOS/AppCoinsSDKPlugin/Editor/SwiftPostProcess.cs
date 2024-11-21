using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

using System.IO;

public static class SwiftPostProcess
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            var projPath = buildPath + "/Unity-Iphone.xcodeproj/project.pbxproj";
            var proj = new PBXProject(); // https://docs.unity3d.com/ScriptReference/iOS.Xcode.PBXProject.html
            proj.ReadFromFile(projPath);

            var targetGuid = proj.TargetGuidByName(PBXProject.GetUnityTestTargetName());

            // var appCoinsGuid = proj.AddRemotePackageReferenceAtVersionUpToNextMajor("https://github.com/Catappult/appcoins-sdk-ios.git", "1.2.0");
            var appCoinsGuid = proj.AddRemotePackageReferenceAtBranch("https://github.com/Catappult/appcoins-sdk-ios.git", "feature/APP-3171_enhance_developer_error");

            var mainTargetGuid = proj.GetUnityMainTargetGuid();
            var frameworkGuid = proj.GetUnityFrameworkTargetGuid();
            proj.AddRemotePackageFrameworkToProject(mainTargetGuid, "AppCoinsSDK", appCoinsGuid, false);
            proj.AddRemotePackageFrameworkToProject(frameworkGuid, "AppCoinsSDK", appCoinsGuid, false);

            proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(targetGuid, "SWIFT_OBJC_BRIDGING_HEADER", "Libraries/Plugins/iOS/UnityIosPlugin/Source/UnityPlugin-Bridging-Header.h");
            proj.SetBuildProperty(targetGuid, "SWIFT_OBJC_INTERFACE_HEADER_NAME", "UnityIosPlugin-Swift.h");
            proj.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks $(PROJECT_DIR)/lib/$(CONFIGURATION) $(inherited)");
            proj.AddBuildProperty(targetGuid, "FRAMERWORK_SEARCH_PATHS",
                "$(inherited) $(PROJECT_DIR) $(PROJECT_DIR)/Frameworks");
            proj.AddBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            proj.AddBuildProperty(targetGuid, "DYLIB_INSTALL_NAME_BASE", "@rpath");
            proj.AddBuildProperty(targetGuid, "LD_DYLIB_INSTALL_NAME",
                "@executable_path/../Frameworks/$(EXECUTABLE_PATH)");
            proj.AddBuildProperty(targetGuid, "DEFINES_MODULE", "YES");
            proj.AddBuildProperty(targetGuid, "SWIFT_VERSION", "4.0");
            proj.AddBuildProperty(targetGuid, "COREML_CODEGEN_LANGUAGE", "Swift");

            proj.WriteToFile(projPath);

            var manager = new ProjectCapabilityManager(projPath, "Unity-iPhone.entitlements", null, proj.GetUnityMainTargetGuid());
            manager.AddKeychainSharing(new string[] { "$(AppIdentifierPrefix)com.aptoide.appcoins-wallet" });
            manager.WriteToFile();

            var entitlementsFilePath = Path.Combine(buildPath, "Unity-iPhone.entitlements");
            AddSDKUrlType(buildPath);
            AddWalletQueriedUrlScheme(buildPath);
            AddCFBundleAllowMixedLocalizations(buildPath);
        }
    }

    static void AddSDKUrlType(string buildPath)
    {
        string plistInfoPath = Path.Combine(buildPath, "Info.plist");
        PlistDocument plistInfo = new();
        plistInfo.ReadFromFile(plistInfoPath);

        var cfBundleURLTypes = plistInfo.root.CreateArray("CFBundleURLTypes");
        PlistElementDict urlTypeDict = cfBundleURLTypes.AddDict();
        urlTypeDict.SetString("CFBundleTypeRole", "Editor");
        PlistElementArray urlSchemesArray = urlTypeDict.CreateArray("CFBundleURLSchemes");
        urlSchemesArray.AddString("$(PRODUCT_BUNDLE_IDENTIFIER).iap");

        plistInfo.WriteToFile(plistInfoPath);
    }

    static void AddWalletQueriedUrlScheme(string buildPath)
    {
        string plistInfoPath = Path.Combine(buildPath, "Info.plist");
        PlistDocument plistInfo = new();
        plistInfo.ReadFromFile(plistInfoPath);

        var lsApplicationQueriesSchemes = plistInfo.root.CreateArray("LSApplicationQueriesSchemes");
        lsApplicationQueriesSchemes.AddString("com.aptoide.appcoins-wallet");

        plistInfo.WriteToFile(plistInfoPath);
    }

    static void AddCFBundleAllowMixedLocalizations(string buildPath)
    {
        string plistInfoPath = Path.Combine(buildPath, "Info.plist");
        PlistDocument plistInfo = new();
        plistInfo.ReadFromFile(plistInfoPath);
        plistInfo.root.SetBoolean("CFBundleAllowMixedLocalizations", true);
        plistInfo.WriteToFile(plistInfoPath);
    }
}
