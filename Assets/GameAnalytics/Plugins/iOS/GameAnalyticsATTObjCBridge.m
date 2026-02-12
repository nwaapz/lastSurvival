#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import "GameAnalyticsATTObjCBridge.h"


void GameAnalyticsRequestTrackingAuthorization(
    GameAnalyticsATTListenerNotDetermined gameAnalyticsATTListenerNotDetermined,
    GameAnalyticsATTListenerRestricted gameAnalyticsATTListenerRestricted,
    GameAnalyticsATTListenerDenied gameAnalyticsATTListenerDenied,
    GameAnalyticsATTListenerAuthorized gameAnalyticsATTListenerAuthorized)
{
    if (@available(iOS 14, *)) {
#if defined(__IPHONE_14_0) || defined(__TVOS_14_0)
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            switch (status)
                {
                case ATTrackingManagerAuthorizationStatusNotDetermined:
                    gameAnalyticsATTListenerNotDetermined();
                    break;
                case ATTrackingManagerAuthorizationStatusRestricted:
                    gameAnalyticsATTListenerRestricted();
                    break;
                case ATTrackingManagerAuthorizationStatusDenied:
                    gameAnalyticsATTListenerDenied();
                    break;
                case ATTrackingManagerAuthorizationStatusAuthorized:
                    gameAnalyticsATTListenerAuthorized();
                    break;
             }
        }];
#endif
    }
    else
    {
        gameAnalyticsATTListenerNotDetermined();
    }
}
