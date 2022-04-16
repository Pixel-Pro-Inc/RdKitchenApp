using System;

using Android.App;
using Android.Content;
using RdKitchenApp.Interfaces;
using RdKitchenApp.Droid;
using Xamarin.Forms;

using Android.Media;
using AndroidX.Core.App;

[assembly:Dependency(typeof(NotificationHelper))]
namespace RdKitchenApp.Droid
{
    class NotificationHelper : INotification
    {
        private Context mContext;
        private NotificationManager mNotificationManager;
        private NotificationCompat.Builder mBuilder;
        public static String NOTIFICATION_CHANNEL_ID = "10023";

        public NotificationHelper()
        {
            mContext = global::Android.App.Application.Context;
        }
        public void CreateNotification()
        {
            string title = "New Order";
            string message = "A new order has arrived!";

            try
            {
                var intent = new Intent(mContext, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop);
                intent.PutExtra(title, message);
                var pendingIntent = PendingIntent.GetActivity(mContext, 0, intent, PendingIntentFlags.OneShot);

                var sound = global::Android.Net.Uri.Parse(ContentResolver.SchemeAndroidResource + "://" + mContext.PackageName + "/" + Resource.Raw.notification);
                // Creating an Audio Attribute
                var alarmAttributes = new AudioAttributes.Builder()
                    .SetContentType(AudioContentType.Sonification)
                    .SetUsage(AudioUsageKind.Notification).Build();

                mBuilder = new NotificationCompat.Builder(mContext);
                mBuilder.SetSmallIcon(Resource.Drawable.icon);
                mBuilder.SetContentTitle(title)
                        .SetSound(sound)
                        .SetAutoCancel(true)
                        .SetContentTitle(title)
                        .SetContentText(message)
                        .SetChannelId(NOTIFICATION_CHANNEL_ID)
                        .SetPriority((int)NotificationPriority.High)
                        .SetVibrate(new long[0])
                        .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate)
                        .SetVisibility((int)NotificationVisibility.Public)
                        .SetSmallIcon(Resource.Drawable.icon)
                        .SetContentIntent(pendingIntent);



                NotificationManager notificationManager = mContext.GetSystemService(Context.NotificationService) as NotificationManager;

                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    NotificationImportance importance = global::Android.App.NotificationImportance.High;

                    NotificationChannel notificationChannel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, title, importance);
                    notificationChannel.EnableLights(true);
                    notificationChannel.EnableVibration(true);
                    notificationChannel.SetSound(sound, alarmAttributes);
                    notificationChannel.SetShowBadge(true);
                    notificationChannel.Importance = NotificationImportance.High;
                    notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });

                    if (notificationManager != null)
                    {
                        mBuilder.SetChannelId(NOTIFICATION_CHANNEL_ID);
                        notificationManager.CreateNotificationChannel(notificationChannel);
                    }
                }

                notificationManager.Notify(0, mBuilder.Build());
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}