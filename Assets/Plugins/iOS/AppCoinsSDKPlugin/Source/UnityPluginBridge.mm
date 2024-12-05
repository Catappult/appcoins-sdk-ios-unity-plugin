#import <Foundation/Foundation.h>
#import "UnityFramework/UnityFramework-Swift.h"

typedef void (*JsonCallback)(const char *json);
typedef void (*PurchaseJsonCallback)(const char *jsonSuccess, const char *jsonError);

extern "C" {
    void _handleDeepLink(const char *url, JsonCallback callback) {
        NSString *urlString = [NSString stringWithUTF8String:url];

        [UnityPlugin.shared handleDeepLinkWithUrl:urlString completion:^(NSDictionary *data) {
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

    void _getProducts(const char **skus, int count, PurchaseJsonCallback callback) {
        NSMutableArray *skuArray = [NSMutableArray arrayWithCapacity:count];
        for (int i = 0; i < count; i++) {
            [skuArray addObject:[NSString stringWithUTF8String:skus[i]]];
        }
        
        NSArray *skuNSArray = [skuArray copy];
    
        [UnityPlugin.shared getProductsWithSkus:skuNSArray completion:^(NSArray * _Nullable success, NSDictionary * _Nullable sdkError) {
            NSString *jsonSuccess = nil;
            NSString *jsonError = nil;

            if (success) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:success options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize success JSON: %@", error);
                } else {
                    jsonSuccess = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (sdkError) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:sdkError options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize error JSON: %@", error);
                } else {
                    jsonError = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (callback) {
                callback(jsonSuccess ? [jsonSuccess UTF8String] : NULL, jsonError ? [jsonError UTF8String] : NULL);
            }
        }];
    }

    void _purchase(const char *sku, const char *payload, PurchaseJsonCallback callback) {
        NSString *skuString = [NSString stringWithUTF8String:sku];
        NSString *payloadString = [NSString stringWithUTF8String:payload];
        
        [UnityPlugin.shared purchaseWithSku:skuString payload:payloadString completion:^(NSDictionary * _Nullable success, NSDictionary * _Nullable sdkError) {
            NSString *jsonSuccess = nil;
            NSString *jsonError = nil;

            if (success) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:success options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize success JSON: %@", error);
                } else {
                    jsonSuccess = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (sdkError) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:sdkError options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize error JSON: %@", error);
                } else {
                    jsonError = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (callback) {
                callback(jsonSuccess ? [jsonSuccess UTF8String] : NULL, jsonError ? [jsonError UTF8String] : NULL);
            }
        }];
    }

    void _getAllPurchases(PurchaseJsonCallback callback) {
        [UnityPlugin.shared getAllPurchasesWithCompletion:^(NSArray * _Nullable success, NSDictionary * _Nullable sdkError) {
            NSString *jsonSuccess = nil;
            NSString *jsonError = nil;

            if (success) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:success options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize success JSON: %@", error);
                } else {
                    jsonSuccess = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (sdkError) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:sdkError options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize error JSON: %@", error);
                } else {
                    jsonError = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (callback) {
                callback(jsonSuccess ? [jsonSuccess UTF8String] : NULL, jsonError ? [jsonError UTF8String] : NULL);
            }
        }];
    }

    void _getLatestPurchase(const char *sku, PurchaseJsonCallback callback) {
        NSString *skuString = [NSString stringWithUTF8String:sku];

        [UnityPlugin.shared getLatestPurchaseWithSku:skuString completion:^(NSDictionary * _Nullable success, NSDictionary * _Nullable sdkError) {
            NSString *jsonSuccess = nil;
            NSString *jsonError = nil;

            if (success) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:success options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize success JSON: %@", error);
                } else {
                    jsonSuccess = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (sdkError) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:sdkError options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize error JSON: %@", error);
                } else {
                    jsonError = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (callback) {
                callback(jsonSuccess ? [jsonSuccess UTF8String] : NULL, jsonError ? [jsonError UTF8String] : NULL);
            }
        }];
    }

    void _getUnfinishedPurchases(PurchaseJsonCallback callback) {
        [UnityPlugin.shared getUnfinishedPurchasesWithCompletion:^(NSArray * _Nullable success, NSDictionary * _Nullable sdkError) {
            NSString *jsonSuccess = nil;
            NSString *jsonError = nil;

            if (success) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:success options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize success JSON: %@", error);
                } else {
                    jsonSuccess = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (sdkError) {
                NSError *error = nil;
                NSData *jsonData = [NSJSONSerialization dataWithJSONObject:sdkError options:0 error:&error];
                if (!jsonData) {
                    NSLog(@"Failed to serialize error JSON: %@", error);
                } else {
                    jsonError = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                }
            }

            if (callback) {
                callback(jsonSuccess ? [jsonSuccess UTF8String] : NULL, jsonError ? [jsonError UTF8String] : NULL);
            }
        }];
    }

    void _consumePurchase(const char *sku, JsonCallback callback) {
        NSString *skuString = [NSString stringWithUTF8String:sku];
        
        [UnityPlugin.shared consumePurchaseWithSku:skuString completion:^(NSDictionary *data) {
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

    const char * _Nullable _getTestingWalletAddress() {
        NSString *address = [UnityPlugin.shared getTestingWalletAddress];
        if (address) {
            return [address UTF8String];
        } else {
            return NULL;
        }
    }
}
