// UnityPlugin-Bridging-Header.h

#ifndef UnityPlugin_Bridging_Header_h
#define UnityPlugin_Bridging_Header_h

#include "UnityInterface.h"

#ifdef __cplusplus
extern "C" {
#endif

void UnitySendMessage(const char *obj, const char *method, const char *msg);

#ifdef __cplusplus
}
#endif

#endif /* UnityPlugin_Bridging_Header_h */
