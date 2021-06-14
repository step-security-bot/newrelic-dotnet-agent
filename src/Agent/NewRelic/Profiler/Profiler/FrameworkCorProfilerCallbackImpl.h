// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "ICorProfilerCallbackBase.h"
#include "../ModuleInjector/ModuleInjector.h"
#include "Module.h"

namespace NewRelic { namespace Profiler
{
    class FrameworkCorProfilerCallbackImpl : public ICorProfilerCallbackBase
    {

    private:
        std::shared_ptr<ModuleInjector::ModuleInjector> _moduleInjector;

    public:
        FrameworkCorProfilerCallbackImpl()
            : ICorProfilerCallbackBase(std::make_shared<SystemCalls>(_X("NEWRELIC_HOME"), _X("NEWRELIC_INSTALL_PATH")))
        {
            GetSingletonish() = this;
            _productName = _X("New Relic .NET Agent");
        }

        ~FrameworkCorProfilerCallbackImpl()
        {
            if (GetSingletonish() == this)
                GetSingletonish() = nullptr;
        }

        // The Framework version of ConfigureEventMask doesn't need the pICorProfilerInfoUnk
        // but the Core version does, hence disabling the warning about 'unreferenced formal parameter'
#pragma warning (push)
#pragma warning (disable : 4100)
        virtual void ConfigureEventMask(IUnknown* pICorProfilerInfoUnk) override
        {
            // register for events that we are interested in getting callbacks for
            LogDebug(L"Calling SetEventMask().");
            ThrowOnError(_corProfilerInfo4->SetEventMask, _eventMask);
        }
#pragma warning (pop)

        virtual bool ShouldInstrument(std::shared_ptr<Configuration::Configuration> configuration, xstring_t processPath, xstring_t /* commandLine */, xstring_t appPoolId) override
        {
            return configuration->ShouldInstrumentNetFramework(processPath, appPoolId);
        }

        virtual xstring_t GetRuntimeExtensionsDirectoryName() override
        {
            return _X("netframework");
        }

        virtual HRESULT MinimumDotnetVersionCheck(IUnknown* pICorProfilerInfoUnk) override
        {
            CComPtr<ICorProfilerInfo4> temp;
            HRESULT interfaceCheckResult = pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo4), (void**)&temp);
            if (FAILED(interfaceCheckResult)) {
                LogError(_X(".NET Framework 4.5 is required.  Detaching New Relic profiler."));
                return CORPROF_E_PROFILER_CANCEL_ACTIVATION;
            }

            return S_OK;
        }
    };
}}
