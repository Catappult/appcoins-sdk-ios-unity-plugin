
#import <Foundation/Foundation.h>
#include "UnityFramework/UnityFramework-Swift.h"


typedef void (*JsonCallback)(const char *json);

extern "C" {
    void _initialize(JsonCallback callback) {
        [UnityPlugin.shared initializeWithCompletion:^(NSArray<NSDictionary *> *products) {
            NSError *error = nil;
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:products options:0 error:&error];
            if (!jsonData) {
                NSLog(@"Failed to serialize products to JSON: %@", error);
                if (callback) {
                    callback(""); // Call with an empty string or error message
                }
                return;
            }
            
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            if (callback) {
                callback([jsonString UTF8String]);
            }
        }];
    }


    void _purchase(const char *sku, JsonCallback callback) {
        NSString *skuString = [NSString stringWithUTF8String:sku];
        [UnityPlugin.shared purchaseWithSKU:skuString completion:^(NSString *result) {
            if (callback) {
                callback([result UTF8String]);
            }
        }];
    }

    void _listPurchases(JsonCallback callback) {
        [UnityPlugin.shared listPurchasesWithCompletion:^(NSArray<NSDictionary *> *purchases) {
            NSError *error = nil;
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:purchases options:0 error:&error];
            if (!jsonData) {
                NSLog(@"Failed to serialize products to JSON: %@", error);
                if (callback) {
                    callback(""); // Call with an empty string or error message
                }
                return;
            }
            
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            if (callback) {
                callback([jsonString UTF8String]);
            }
        }];
    }
}
