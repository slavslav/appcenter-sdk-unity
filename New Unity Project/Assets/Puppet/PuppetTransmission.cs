// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Unity.Analytics;
using Microsoft.AppCenter.Unity;
using UnityEngine;
using UnityEngine.UI;

public class PuppetTransmission : MonoBehaviour
{
    public Toggle ParentTransmissionEnabled;
    public Toggle ChildTransmissionEnabled;
    public Toggle CollectDeviceIdChild;
    public Toggle CollectDeviceIdParent;
    public InputField EventName;
    public InputField AppNameParent;
    public InputField AppVersionParent;
    public InputField AppLocaleParent;
    public InputField AppNameChild;
    public InputField AppVersionChild;
    public InputField AppLocaleChild;
    public InputField TransmissionTarget;
    public InputField ChildTransmissionTarget;
    public InputField ChildUserId;
    public InputField ParentUserId;
    public GameObject EventParentProperty;
    public GameObject EventChildProperty;
    public RectTransform EventParentPropertiesList;
    public RectTransform EventChildPropertiesList;
    public Text TransmissionStatus;
    public Text ChildTransmissionStatus;
    public Toggle IsCritical;
    private string _parentTransmissionTargetToken = "f4c077c9fd0641f4b2d53beaf99c7f6e-029eddb7-7fae-4aa3-b1f8-c6cfcb0fa470-7450";
    private string _childTransmissionTargetToken = "de994d374a7d4210ac2c148e61417680-5d57d718-d749-468b-8483-be21b5945fe0-7149";
    private bool _isCritical;

    private void OnEnable()
    {
        AppCenter.StartFromLibrary(new Type[]{ AppCenter.Analytics });
        TransmissionTarget.text = _parentTransmissionTargetToken;
        ChildTransmissionTarget.text = _childTransmissionTargetToken;

        var transmissionTargetParent = Analytics.GetTransmissionTarget(ResolveParentToken());
        if (transmissionTargetParent == null)
        {
            ParentTransmissionEnabled.isOn = false;
            ChildTransmissionEnabled.isOn = false;
            return;
        }

        StartCoroutine(IsParentEnabledCoroutine(transmissionTargetParent));
        TransmissionTarget childTransmissionTarget = transmissionTargetParent.GetTransmissionTarget(ResolveChildToken());
        if (childTransmissionTarget != null)
        {
            StartCoroutine(IsChildEnabledCoroutine(childTransmissionTarget));
        }
        CollectDeviceIdChild.isOn = false;
        CollectDeviceIdParent.isOn = false;
    }

    private IEnumerator IsChildEnabledCoroutine(TransmissionTarget childTransmissionTarget)
    {
        var task = childTransmissionTarget.IsEnabledAsync();
        yield return task;
        ChildTransmissionEnabled.isOn = task.Result;
    }

    private IEnumerator IsParentEnabledCoroutine(TransmissionTarget transmissionTarget)
    {
        var task = transmissionTarget.IsEnabledAsync();
        yield return task;
        ParentTransmissionEnabled.isOn = task.Result;
    }

    public void SetIsCritical(bool critical)
    {
        _isCritical = IsCritical.isOn;
    }

    private string ResolveParentToken()
    {
        if (string.IsNullOrEmpty(TransmissionTarget.text))
        {
            return _parentTransmissionTargetToken;
        }
        else
        {
            return TransmissionTarget.text;
        }
    }

    private string ResolveChildToken()
    {
        if (string.IsNullOrEmpty(ChildTransmissionTarget.text))
        {
            return _childTransmissionTargetToken;
        }
        else
        {
            return ChildTransmissionTarget.text;
        }
    }

    public void SetParentCollectDeviceID(bool enabled)
    {
        var transmissionTarget = Analytics.GetTransmissionTarget(ResolveParentToken());
        if (transmissionTarget == null)
        {
            Debug.Log("Transmission target is null.");
            return;
        }
        if (enabled)
        {
            transmissionTarget.GetPropertyConfigurator().CollectDeviceId();
            CollectDeviceIdParent.enabled = false;
        }
    }

    public void SetChildCollectDeviceID(bool enabled)
    {
        var transmissionTarget = Analytics.GetTransmissionTarget(ResolveParentToken());
        if (transmissionTarget == null)
        {
            Debug.Log("Transmission target is null.");
            return;
        }
        var childTransmissionTarget = transmissionTarget.GetTransmissionTarget(ResolveChildToken());
        if (enabled)
        {
            childTransmissionTarget.GetPropertyConfigurator().CollectDeviceId();
            CollectDeviceIdChild.enabled = false;
        }
    }

    public void SetChildTransmissionEnabled(bool enabled)
    {
        StartCoroutine(SetChildTransmissionEnabledCoroutine(enabled));
    }

    public void SetParentTransmissionEnabled(bool enabled)
    {
        StartCoroutine(SetParentTransmissionEnabledCoroutine(enabled));
    }

    private IEnumerator SetParentTransmissionEnabledCoroutine(bool enabled)
    {
        var transmissionTarget = Analytics.GetTransmissionTarget(ResolveParentToken());
        if (transmissionTarget == null)
        {
            Debug.Log("Transmission target is null.");
            yield break;
        }
        yield return transmissionTarget.SetEnabledAsync(enabled);
        var isEnabled = transmissionTarget.IsEnabledAsync();
        yield return isEnabled;
        ParentTransmissionEnabled.isOn = isEnabled.Result;
    }

    private IEnumerator SetChildTransmissionEnabledCoroutine(bool enabled)
    {
        var transmissionTarget = Analytics.GetTransmissionTarget(ResolveParentToken());
        if (transmissionTarget == null)
        {
            Debug.Log("Transmission target is null.");
            yield break;
        }
        var childTransmissionTarget = transmissionTarget.GetTransmissionTarget(ResolveChildToken());
        yield return childTransmissionTarget.SetEnabledAsync(enabled);
        var isEnabled = childTransmissionTarget.IsEnabledAsync();
        yield return isEnabled;
        ChildTransmissionEnabled.isOn = isEnabled.Result;
    }

    public void AddParentProperty()
    {
        var property = Instantiate(EventParentProperty);
        property.transform.SetParent(EventParentPropertiesList, false);
    }

    public void AddChildProperty()
    {
        var property = Instantiate(EventChildProperty);
        property.transform.SetParent(EventChildPropertiesList, false);
    }

    private void OverrideParentProperties(TransmissionTarget transmissionTarget)
    {
        OverrideProperties(transmissionTarget, AppNameParent, AppVersionParent, AppLocaleParent);
    }

    private void OverrideChildProperties(TransmissionTarget transmissionTarget)
    {
        OverrideProperties(transmissionTarget, AppNameChild, AppVersionChild, AppLocaleChild);
    }

    private void OverrideProperties(TransmissionTarget transmissionTarget, InputField appName, InputField appVersion, InputField appLocale)
    {
        var overridenAppName = string.IsNullOrEmpty(appName.text) ? null : appName.text;
        var overridenAppVersion = string.IsNullOrEmpty(appVersion.text) ? null : appVersion.text;
        var overridenAppLocale = string.IsNullOrEmpty(appLocale.text) ? null : appLocale.text;
        var propertyConfigurator = transmissionTarget.GetPropertyConfigurator();
        propertyConfigurator.SetAppName(overridenAppName);
        propertyConfigurator.SetAppVersion(overridenAppVersion);
        propertyConfigurator.SetAppLocale(overridenAppLocale);
    }

    public void OnParentUserIdChanged(string newUserId)
    {
        var transmissionTarget = GetParentTransmissionTarget();
        if (transmissionTarget != null)
        {
            var propertyConfigurator = transmissionTarget.GetPropertyConfigurator();
            propertyConfigurator.SetUserId(newUserId);
        }
    }

    public void OnChildUserIdChanged(string newUserId)
    {
        var transmissionTarget = GetChildTransmissionTarget();
        if (transmissionTarget != null)
        {
            var propertyConfigurator = transmissionTarget.GetPropertyConfigurator();
            propertyConfigurator.SetUserId(newUserId);
        }
    }

    public void PauseParentTransmission()
    {
        var transmissionTarget = GetParentTransmissionTarget();
        if (transmissionTarget != null)
        {
            Debug.Log("Pausing the parent transmission...");
            transmissionTarget.Pause();
            TransmissionStatus.text = "Transmission paused.";
        }
    }

    public void ResumeParentTransmission()
    {
        var transmissionTarget = GetParentTransmissionTarget();
        if (transmissionTarget != null)
        {
            Debug.Log("Resuming the parent transmission...");
            transmissionTarget.Resume();
            TransmissionStatus.text = "Transmission resumed.";
        }
    }

    public void PauseChildTransmission()
    {
        var transmissionTarget = GetChildTransmissionTarget();
        if (transmissionTarget != null)
        {
            Debug.Log("Pausing the child transmission...");
            transmissionTarget.Pause();
            ChildTransmissionStatus.text = "Child transmission paused.";
        }
    }

    public void ResumeChildTransmission()
    {
        var transmissionTarget = GetChildTransmissionTarget();
        if (transmissionTarget != null)
        {
            Debug.Log("Resuming the child transmission...");
            transmissionTarget.Resume();
            ChildTransmissionStatus.text = "Child transmission resumed.";
        }
    }

    private TransmissionTarget GetParentTransmissionTarget()
    {
        var transmissionTarget = Analytics.GetTransmissionTarget(ResolveParentToken());
        if (transmissionTarget != null)
        {
            OverrideParentProperties(transmissionTarget);
            return transmissionTarget;
        }
        Debug.Log("Transmission target is null.");
        return null;
    }

    private TransmissionTarget GetChildTransmissionTarget()
    {
        var transmissionTarget = GetParentTransmissionTarget();
        if (transmissionTarget != null)
        {
            var childTransmissionTarget = transmissionTarget.GetTransmissionTarget(ResolveChildToken());
            OverrideChildProperties(childTransmissionTarget);
            if (childTransmissionTarget != null)
            {
                return childTransmissionTarget;
            }
            Debug.Log("Child transmission target is null.");
        }
        return null;
    }

    public void ClearParentUserId()
    {
        ParentUserId.text = "";
        OnParentUserIdChanged(null);
    }

    public void ClearChildUserId()
    {
        ChildUserId.text = "";
        OnChildUserIdChanged(null);
    }

    private PropertyConfigurator ConfigurePropertiesToString(TransmissionTarget transmissionTarget, RectTransform eventProperties)
    {
        var properties = PropertiesHelper.GetStringProperties(eventProperties);
        var propertyConfigurator = transmissionTarget.GetPropertyConfigurator();
        if (properties != null) 
        {
            foreach (var property in properties)
            {
                propertyConfigurator.SetEventProperty(property.Key, property.Value);
            }
        }
        propertyConfigurator.SetEventProperty("extraEventProperty", "should be removed!");
        propertyConfigurator.RemoveEventProperty("extraEventProperty");
        return propertyConfigurator;
    }

    private PropertyConfigurator ConfigurePropertiesToType(TransmissionTarget transmissionTarget, RectTransform eventProperties)
    {
        var propertyConfigurator = transmissionTarget.GetPropertyConfigurator();
        PropertiesHelper.AddPropertiesToPropertyConfigurator(eventProperties, propertyConfigurator);
        propertyConfigurator.SetEventProperty("extraEventProperty", "should be removed!");
        propertyConfigurator.RemoveEventProperty("extraEventProperty");
        return propertyConfigurator;
    }

    public void TrackEventParentStringPropertiesTransmission()
    {
        var transmissionTarget = GetParentTransmissionTarget();
        if (transmissionTarget != null)
        {
            OverrideParentProperties(transmissionTarget);
            var properties = PropertiesHelper.GetStringProperties(EventParentPropertiesList);
            if (properties == null)
            {
                if (_isCritical)
                {
                    IDictionary<string, string> nullProps = null;
                    transmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    transmissionTarget.TrackEvent(EventName.text);
                }
            }
            else
            {
                var propertyConfigurator = ConfigurePropertiesToString(transmissionTarget, EventParentPropertiesList);
                if (_isCritical)
                {
                    IDictionary<string, string> nullProps = null;
                    transmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    transmissionTarget.TrackEvent(EventName.text);
                }
                PropertiesHelper.RemovePropertiesFromConfigurator(EventParentPropertiesList, propertyConfigurator);
            }
        }
    }

    public void TrackEventParentTypedPropertiesTransmission()
    {
        var transmissionTarget = GetParentTransmissionTarget();
        if (transmissionTarget != null)
        {
            OverrideParentProperties(transmissionTarget);
            var properties = PropertiesHelper.GetTypedProperties(EventParentPropertiesList);
            if (properties == null)
            {
                if (_isCritical)
                {
                    EventProperties nullProps = null;
                    transmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    transmissionTarget.TrackEvent(EventName.text);
                }
            }
            else
            {
                var propertyConfigurator = ConfigurePropertiesToType(transmissionTarget, EventParentPropertiesList);
                if (_isCritical)
                {
                    EventProperties nullProps = null;
                    transmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    transmissionTarget.TrackEvent(EventName.text);
                }
                PropertiesHelper.RemovePropertiesFromConfigurator(EventParentPropertiesList, propertyConfigurator);
            }
        }
    }

    public void TrackEventStringPropertiesChildTransmission()
    {
        var parentTransmissionTarget = GetParentTransmissionTarget();
        PropertyConfigurator parentPropertyConfigurator = null;
        if (parentTransmissionTarget != null)
        {
            parentPropertyConfigurator = ConfigurePropertiesToString(parentTransmissionTarget, EventParentPropertiesList);
        }
        var childTransmissionTarget = GetChildTransmissionTarget();
        if (childTransmissionTarget != null)
        {
            OverrideChildProperties(childTransmissionTarget);
            var properties = PropertiesHelper.GetStringProperties(EventChildPropertiesList);
            if (properties == null)
            {
                if (_isCritical)
                {
                    IDictionary<string, string> nullProps = null;
                    childTransmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    childTransmissionTarget.TrackEvent(EventName.text);
                }
            }
            else
            {
                var propertyConfigurator = ConfigurePropertiesToString(childTransmissionTarget, EventChildPropertiesList);
                if (_isCritical)
                {
                    IDictionary<string, string> nullProps = null;
                    childTransmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    childTransmissionTarget.TrackEvent(EventName.text);
                }
                PropertiesHelper.RemovePropertiesFromConfigurator(EventParentPropertiesList, propertyConfigurator);
            }
        }
        PropertiesHelper.RemovePropertiesFromConfigurator(EventParentPropertiesList, parentPropertyConfigurator);
    }

    public void TrackEventTypedPropertiesChildTransmission()
    {
        var parentTransmissionTarget = GetParentTransmissionTarget();
        PropertyConfigurator parentPropertyConfigurator = null;
        if (parentTransmissionTarget != null)
        {
            parentPropertyConfigurator = ConfigurePropertiesToString(parentTransmissionTarget, EventParentPropertiesList);
        }
        var childTransmissionTarget = GetChildTransmissionTarget();
        if (childTransmissionTarget != null)
        {
            OverrideChildProperties(childTransmissionTarget);
            var properties = PropertiesHelper.GetTypedProperties(EventChildPropertiesList);
            if (properties == null)
            {
                if (_isCritical)
                {
                    EventProperties nullProps = null;
                    childTransmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    childTransmissionTarget.TrackEvent(EventName.text);
                }
            }
            else
            {
                var propertyConfigurator = ConfigurePropertiesToString(childTransmissionTarget, EventChildPropertiesList);
                if (_isCritical)
                {
                    EventProperties nullProps = null;
                    childTransmissionTarget.TrackEvent(EventName.text, nullProps, Flags.PersistenceCritical);
                }
                else
                {
                    childTransmissionTarget.TrackEvent(EventName.text);
                }
                PropertiesHelper.RemovePropertiesFromConfigurator(EventParentPropertiesList, propertyConfigurator);
            }
        }
        PropertiesHelper.RemovePropertiesFromConfigurator(EventParentPropertiesList, parentPropertyConfigurator);
    }
}
