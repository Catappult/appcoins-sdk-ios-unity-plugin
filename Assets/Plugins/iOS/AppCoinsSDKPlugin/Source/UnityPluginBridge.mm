#import <Foundation/Foundation.h>
#import "UnityFramework/UnityFramework-Swift.h"

typedef void (*JsonCallback)(const char *json);

extern "C" {
    void _isAvailable(JsonCallback callback) {
        [UnityPlugin.shared isAvailableWithCompletion:^(NSDictionary *data) {
            NSError *error = nil;
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:data options:0 error:&error];
            if (!jsonData) {
                NSLog(@"Failed to serialize JSON: %@", error);
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

    void _getProducts(const char **skus, int count, JsonCallback callback) {
        NSMutableArray *skuArray = [NSMutableArray arrayWithCapacity:count];
        for (int i = 0; i < count; i++) {
            [skuArray addObject:[NSString stringWithUTF8String:skus[i]]];
        }
        
        NSArray *skuNSArray = [skuArray copy];
        [UnityPlugin.shared getProductsWithSkus:skuNSArray completion:^(NSArray *data) {
            NSError *error = nil;
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:data options:0 error:&error];
            if (!jsonData) {
                NSLog(@"Failed to serialize JSON: %@", error);
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
        
        [UnityPlugin.shared purchaseWithSku:skuString completion:^(NSDictionary *data) {
            NSError *error = nil;
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:data options:0 error:&error];
            if (!jsonData) {
                NSLog(@"Failed to serialize JSON: %@", error);
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
