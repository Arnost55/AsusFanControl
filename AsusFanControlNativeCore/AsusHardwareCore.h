#pragma once

#include "AsusWinIoBindings.h"
#include "HardwareSnapshot.h"

#include <string>

namespace AsusFanControlNativeCore
{
class AsusHardwareCore
{
public:
    HardwareSnapshot ReadSnapshot();

    OperationResult ApplySpeedPercent(int percent);

    OperationResult DisableControl();

private:
    OperationResult ApplySpeedPercentInternal(int percent);
    OperationResult EnsureLoaded();

    AsusWinIoBindings bindings_;
};
}
