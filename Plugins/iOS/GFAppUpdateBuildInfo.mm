#import <Foundation/Foundation.h>

extern "C" const char* GFApplicationGetIOSBuildNumber()
{
    NSString *buildNumber = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleVersion"];
    if (buildNumber == nil)
    {
        return "";
    }

    return [buildNumber UTF8String];
}
