#import <UIKit/UIKit.h>
#import <FBAudienceNetwork/FBAdSettings.h>
#import <FBSDKCoreKit/FBSDKSettings.h>
//
//  IOSHelper.m
//  Unity-iPhone
//
//  Created by Tuan Nguyen on 5/8/20.
//
#import "IOSHelper.h"

@implementation IOSHelper

static NSString *notificationData;

+(void) setNotificationData:(NSString *)data{
    notificationData = data;
}

+(NSString *) getNotificationData{
    return notificationData;
}

@end

extern "C"{
    char* MakeStringCopy (NSString* nsstring)
    {
        if (nsstring == NULL) {
            return NULL;
        }
        // convert from NSString to char with utf8 encoding
        const char* string = [nsstring cStringUsingEncoding:NSUTF8StringEncoding];
        if (string == NULL) {
            return NULL;
        }

        // create char copy with malloc and strcpy
        char* res = (char*)malloc(strlen(string) + 1);
        strcpy(res, string);
        return res;
    }
    
    const char* GetSettingsURL () {
         NSURL * url = [NSURL URLWithString: UIApplicationOpenSettingsURLString];
         return MakeStringCopy(url.absoluteString);
    }
    
    void NativeOpenAppSettings () {
        NSURL * url = [NSURL URLWithString: UIApplicationOpenSettingsURLString];
        [[UIApplication sharedApplication] openURL:url options:@{} completionHandler:nil];
    }

    bool NativeIsAppInstalled(const char* bundleIdentifier){
        NSString* scheme = [[NSString stringWithCString:bundleIdentifier encoding: NSASCIIStringEncoding]  stringByAppendingString: @"://"];
        NSURL* url = [NSURL URLWithString:scheme];
        return [[UIApplication sharedApplication] canOpenURL:url];
    }

    char* cStringCopy(const char* string)
    {
        if (string == NULL)
            return NULL;
        
        char* res = (char*)malloc(strlen(string) + 1);
        strcpy(res, string);
        
        return res;
    }

    char* NativeGetNotificationData(){
        char *ret = cStringCopy([[IOSHelper getNotificationData] UTF8String]);
        [IOSHelper setNotificationData:nil];
        return ret;
    }

        void EnableFBAdvertiserTracking(){
        if(@available(iOS 14, *)){
            [FBAdSettings setAdvertiserTrackingEnabled:YES];
            [[FBSDKSettings sharedSettings] setAdvertiserTrackingEnabled:YES];
        }
    }

}
