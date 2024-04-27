using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

using System.IO;
using UnityEngine;
using System.Net.NetworkInformation;
using Unity.VisualScripting;

public static class SwiftPostProcess
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            var projPath = buildPath + "/Unity-Iphone.xcodeproj/project.pbxproj";
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            var targetGuid = proj.TargetGuidByName(PBXProject.GetUnityTestTargetName());

            var appCoinsGuid = proj.AddRemotePackageReferenceAtVersionUpToNextMajor("https://github.com/Catappult/appcoins-sdk-ios.git", "1.0.0");
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
            AddStoreKitExternalPurchaseCapability(projPath, entitlementsFilePath);
            AddStoreKitExternalPurchaseCapabilityCountries(buildPath);
            AddSDKUrlType(buildPath);
            AddWalletQueriedUrlScheme(buildPath);
        }
    }

    static void AddStoreKitExternalPurchaseCapability(string projPath, string entitlementsFile)
    {
        string entitlementsPath = Path.Combine(projPath, entitlementsFile);
        PlistDocument plistEntitlements = new PlistDocument();
        if (File.Exists(entitlementsPath))
        {
            plistEntitlements.ReadFromFile(entitlementsPath);
        }
        else
        {
            plistEntitlements.Create();
        }

        PlistElementDict rootDict = plistEntitlements.root;
        rootDict.SetBoolean("com.apple.developer.storekit.external-purchase", true);

        plistEntitlements.WriteToFile(entitlementsPath);
    }

    static void AddStoreKitExternalPurchaseCapabilityCountries(string buildPath)
    {
        string plistInfoPath = Path.Combine(buildPath, "Info.plist");
        PlistDocument plistInfo = new();
        plistInfo.ReadFromFile(plistInfoPath);

        var skExternalPurchase = plistInfo.root.CreateArray("SKExternalPurchase");

        List<string> countryCodes = new()
                        {
                            "at", "be", "bg", "hr", "cy", "cz", "dk", "ee", "fi", "fr",
                            "de", "gr", "hu", "ie", "it", "lv", "lt", "lu", "mt", "nl",
                            "pl", "pt", "ro", "sk", "si", "es", "se"
                        };


        foreach (var countryCode in countryCodes)
        {
            skExternalPurchase.AddString(countryCode);
        }

        plistInfo.WriteToFile(plistInfoPath);
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
}
