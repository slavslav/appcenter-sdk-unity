﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AOT;
using Microsoft.AppCenter.Unity.Crashes;
using UnityEngine;
using UnityEngine.UI;
using Exception = Microsoft.AppCenter.Unity.Crashes.Models.Exception;

public class PuppetCrashes : MonoBehaviour
{
    private static bool _crashesNativeCallbackRegistered;

    public Toggle CrashesEnabled;
    public Toggle ReportUnhandledExceptions;
    public Toggle EnableUnhandledExceptionAttachments;
    public Text LastSessionCrashReport;
    public InputField TextAttachment;
    public Toggle BinaryAttachment;
    public Text LowMemoryLabel;

    void OnEnable()
    {
        ReportUnhandledExceptions.isOn = Crashes.IsReportingUnhandledExceptions();
        EnableUnhandledExceptionAttachments.interactable = ReportUnhandledExceptions.isOn;
        TextAttachment.text = PuppetAppCenter.TextAttachmentCached;
        BinaryAttachment.isOn = PuppetAppCenter.BinaryAttachmentCached;
        StartCoroutine(OnEnableCoroutine());
    }

    private IEnumerator OnEnableCoroutine()
    {
        var isEnabled = Crashes.IsEnabledAsync();
        yield return isEnabled;
        CrashesEnabled.isOn = isEnabled.Result;
        var hasLowMemoryWarning = Crashes.HasReceivedMemoryWarningInLastSessionAsync();
        yield return hasLowMemoryWarning;
        LowMemoryLabel.text = hasLowMemoryWarning.Result ? "Yes" : "No";
#if UNITY_ANDROID
        if (!_crashesNativeCallbackRegistered)
        {
            var minidumpDir = Crashes.GetMinidumpDirectoryAsync();
            yield return minidumpDir;
            setupNativeCrashesListener(minidumpDir.Result);
            _crashesNativeCallbackRegistered = true;
        }
#endif
    }

    public void TestCrash()
    {
        Crashes.GenerateTestCrash();
    }

    public void OnValueChanged()
    {
        PuppetAppCenter.TextAttachmentCached = TextAttachment.text;
        PlayerPrefs.SetString(PuppetAppCenter.TextAttachmentKey, TextAttachment.text);
        PlayerPrefs.Save();
    }

    public void OnBinaryValueChanged()
    {
        PuppetAppCenter.BinaryAttachmentCached = BinaryAttachment.isOn;
        PlayerPrefs.SetInt(PuppetAppCenter.BinaryAttachmentKey, BinaryAttachment.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetCrashesEnabled(bool enabled)
    {
        StartCoroutine(SetCrashesEnabledCoroutine(enabled));
    }

    public void SetReportUnhandledExceptions()
    {
        Crashes.ReportUnhandledExceptions(ReportUnhandledExceptions.isOn, EnableUnhandledExceptionAttachments.isOn);
        EnableUnhandledExceptionAttachments.interactable = ReportUnhandledExceptions.isOn;
        EnableUnhandledExceptionAttachments.isOn = EnableUnhandledExceptionAttachments.isOn && ReportUnhandledExceptions.isOn;
    }

    private IEnumerator SetCrashesEnabledCoroutine(bool enabled)
    {
        yield return Crashes.SetEnabledAsync(enabled);
        var isEnabled = Crashes.IsEnabledAsync();
        yield return isEnabled;
        CrashesEnabled.isOn = isEnabled.Result;
    }

    public void TestHandledError()
    {
        try
        {
            throw new System.Exception("Test error");
        }
        catch (System.Exception ex)
        {
            var properties = new Dictionary<string, string> { { "Category", "Music" }, { "Wifi", "On" } };
            Crashes.TrackError(ex, properties, GetErrorAttachments());
        }
    }

    public void TriggerLowMemoryWarning()
    {
        StartCoroutine(LowMemoryTrigger());
    }

    private IEnumerator LowMemoryTrigger()
    {
        var list = new List<byte[]>();
        while (true)
        {
            list.Add(new byte[1024 * 1024 * 128]);
            yield return null;
        }
    }

    public void DivideByZero()
    {
        Debug.Log(42 / int.Parse("0"));
    }

    public void NullReferenceException()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        nativeCrashNullPointer();
#else
        string str = null;
        Debug.Log(str.Length);
#endif
    }

    public void ExceptionInNewThread()
    {
#if !UNITY_WSA_10_0
        new Thread(() =>
        {
            Thread.Sleep(3000);
            object obj = null;
            obj.ToString();
        }).Start();
#endif
    }

    public void LastCrashReport()
    {
        StartCoroutine(LastCrashReportCoroutine());
    }

    public static ErrorAttachmentLog[] GetErrorAttachmentstHandler(ErrorReport errorReport)
    {
        Debug.LogFormat("GetErrorAttachments for error report with ID: {0}. IsCrash={1}", errorReport.Id, errorReport.IsCrash);
        return GetErrorAttachments();
    }

    private static ErrorAttachmentLog[] GetErrorAttachments()
    {
        PuppetAppCenter.StorageReadyEvent.WaitOne();
        var attachments = new List<ErrorAttachmentLog>();
        if (!string.IsNullOrEmpty(PuppetAppCenter.TextAttachmentCached))
        {
            attachments.Add(ErrorAttachmentLog.AttachmentWithText(PuppetAppCenter.TextAttachmentCached, "hello.txt"));
        }
        if (PuppetAppCenter.BinaryAttachmentCached)
        {
            attachments.Add(PuppetAttachmentHelper.getSampleBinaryAttachmentLog());
        }
        return attachments.Count == 0 ? null : attachments.ToArray();
    }

    [MonoPInvokeCallback(typeof(Crashes.ShouldProcessErrorReportHandler))]
    public static bool ShouldProcessErrorReportHandler(ErrorReport errorReport)
    {
        return true;
    }

    [MonoPInvokeCallback(typeof(Crashes.SendingErrorReportHandler))]
    public static void SendingErrorReportHandler(ErrorReport errorReport)
    {
        Debug.Log("Puppet SendingErrorReportHandler");
    }

    [MonoPInvokeCallback(typeof(Crashes.SentErrorReportHandler))]
    public static void SentErrorReportHandler(ErrorReport errorReport)
    {
        Debug.Log("Puppet SentErrorReportHandler");
    }

    [MonoPInvokeCallback(typeof(Crashes.FailedToSendErrorReportHandler))]
    public static void FailedToSendErrorReportHandler(ErrorReport errorReport, Exception exception)
    {
        Debug.LogFormat("Puppet FailedToSendErrorReportHandler, exception message: {0}", exception.Message);
    }

    private IEnumerator LastCrashReportCoroutine()
    {
        var hasCrashed = Crashes.HasCrashedInLastSessionAsync();
        yield return hasCrashed;
        if (hasCrashed.Result)
        {
            var lastSessionReport = Crashes.GetLastSessionCrashReportAsync();
            yield return lastSessionReport;
            var errorReport = lastSessionReport.Result;
            if (errorReport != null)
            {
                var status = new StringBuilder();
                status.AppendLine("Message: " + errorReport.Exception.Message);
                status.AppendLine("App Start Time: " + errorReport.AppStartTime);
                status.AppendLine("App Error Time: " + errorReport.AppErrorTime);
                status.AppendLine("Report Id: " + errorReport.Id);
                status.AppendLine("Process Id: " + errorReport.ProcessId);
                status.AppendLine("Reporter Key: " + errorReport.ReporterKey);
                status.AppendLine("Reporter Signal: " + errorReport.ReporterSignal);
                status.AppendLine("Is App Killed: " + errorReport.IsAppKill);
                status.AppendLine("Thread Name: " + errorReport.ThreadName);
                status.AppendLine("Stack Trace: " + errorReport.Exception.StackTrace);
                status.AppendLine("IsCrash: " + errorReport.IsCrash);
                if (errorReport.Device != null)
                {
                    status.AppendLine("Device.SdkName: " + errorReport.Device.SdkName);
                    status.AppendLine("Device.SdkVersion: " + errorReport.Device.SdkVersion);
                    status.AppendLine("Device.Model: " + errorReport.Device.Model);
                    status.AppendLine("Device.OemName: " + errorReport.Device.OemName);
                    status.AppendLine("Device.OsName: " + errorReport.Device.OsName);
                    status.AppendLine("Device.OsVersion: " + errorReport.Device.OsVersion);
                    status.AppendLine("Device.OsBuild: " + errorReport.Device.OsBuild);
                    status.AppendLine("Device.OsApiLevel: " + errorReport.Device.OsApiLevel);
                    status.AppendLine("Device.Locale: " + errorReport.Device.Locale);
                    status.AppendLine("Device.TimeZoneOffset: " + errorReport.Device.TimeZoneOffset);
                    status.AppendLine("Device.ScreenSize: " + errorReport.Device.ScreenSize);
                    status.AppendLine("Device.AppVersion: " + errorReport.Device.AppVersion);
                    status.AppendLine("Device.CarrierName: " + errorReport.Device.CarrierName);
                    status.AppendLine("Device.CarrierCountry: " + errorReport.Device.CarrierCountry);
                    status.AppendLine("Device.AppBuild: " + errorReport.Device.AppBuild);
                    status.AppendLine("Device.AppNamespace: " + errorReport.Device.AppNamespace);
                }
                LastSessionCrashReport.text = status.ToString();
            }
            else
            {
                LastSessionCrashReport.text = "App has crashed during the last session but no error report has been found";
            }
        }
        else
        {
            LastSessionCrashReport.text = "App has not crashed during the last session";
        }
    }

    [DllImport("PuppetBreakpad")]
    private static extern void nativeCrashNullPointer();

    [DllImport("PuppetBreakpad")]
    private static extern void setupNativeCrashesListener(string path);
}
