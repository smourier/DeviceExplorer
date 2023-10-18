using System;

namespace DeviceExplorer.Resources.Hid
{
    public static class Hid
    {
        public static Type GetUsageType(this HID_USAGE_PAGE usage)
        {
            switch (usage)
            {
                case HID_USAGE_PAGE.HID_USAGE_PAGE_GENERIC:
                    return typeof(HID_USAGE_GENERIC);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_SIMULATION:
                    return typeof(HID_USAGE_SIMULATION);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_VR:
                    return typeof(HID_USAGE_VR);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_SPORT:
                    return typeof(HID_USAGE_SPORT);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_GAME:
                    return typeof(HID_USAGE_GAME);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_GENERIC_DEVICE:
                    return typeof(HID_USAGE_GENERIC_DEVICE);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_KEYBOARD:
                    return typeof(HID_USAGE_KEYBOARD);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_LED:
                    return typeof(HID_USAGE_LED);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_TELEPHONY:
                    return typeof(HID_USAGE_TELEPHONY);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_CONSUMER:
                    return typeof(HID_USAGE_CONSUMER);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_DIGITIZER:
                    return typeof(HID_USAGE_DIGITIZER);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_HAPTICS:
                    return typeof(HID_USAGE_HAPTICS);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_ALPHANUMERIC:
                    return typeof(HID_USAGE_ALPHANUMERIC);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_LIGHTING_ILLUMINATION:
                    return typeof(HID_USAGE_CAMERA);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_CAMERA_CONTROL:
                    return typeof(HID_USAGE_CAMERA);

                case HID_USAGE_PAGE.HID_USAGE_PAGE_MICROSOFT_BLUETOOTH_HANDSFREE:
                    return typeof(HID_USAGE_MS_BTH_HF);

                default:
                    return null;
            }
        }
    }
}
