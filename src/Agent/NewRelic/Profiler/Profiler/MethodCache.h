#pragma once

#include "stdafx.h"
#include "../Logging/Logger.h"
#include "corprof.h"
#include <mutex>
#include <system_error>
#include "../MethodRewriter/IFunction.h"

namespace NewRelic
{
    namespace Profiler
    {
        class MethodCache
        {

        private:

            static bool _initialized;
            static bool _disabled;

            static std::mutex _lock;
            static std::vector<ModuleID> _modules;
            static std::vector<mdMethodDef> _methods;
                
            static const int MAX_METHOD_CACHE = 100;

        public:

            static bool IsAgentInitialized()
            {
                return _initialized;
            }

            static bool CanUseCache(NewRelic::Profiler::MethodRewriter::IFunctionPtr& function)
            {

                // access violation? why check without lock, then with lock?
                if (_disabled)      return false;
                if (_initialized)   return true;

                _lock.lock();

                if (_disabled)
                {
                    _lock.unlock();
                    return false;
                }

                if (_initialized)
                {
                    _lock.unlock();
                    return true;
                }

                auto count = _modules.size();
                if (count > MAX_METHOD_CACHE)
                {
                    LogWarn(L"Reached maximum (", count, L") pre-Agent initialization method limit - disabling MethodInfo caching.");

                    _disabled = true;

                    // clear memory
                    std::vector<ModuleID>().swap(_modules);
                    std::vector<mdMethodDef>().swap(_methods);

                    _lock.unlock();
                    return false;
                }

                _modules.push_back(function->GetModuleID());
                _methods.push_back(function->GetMethodToken());

                _lock.unlock();
                return false;
            }

            static void EnableCaching(CComPtr<ICorProfilerInfo4>& profiler)
            {
                if (_initialized || _disabled) return;

                _lock.lock();
                _initialized = true;

                auto count = _modules.size();
                auto result = profiler->RequestReJIT((ULONG)count, _modules.data(), _methods.data());

                if (SUCCEEDED(result))
                {
                    LogInfo(L"Successfully requested ", count, L" method(s) for post-Agent initialization reJIT.");
                }
                else
                {
                    LogWarn(L"Failed requesting ", count, L" methods for post-Agent initialization reJIT, disabling MethodInfo caching: ", result);
                    _disabled = true;
                }

                // clear memory
                std::vector<ModuleID>().swap(_modules);
                std::vector<mdMethodDef>().swap(_methods);

                _lock.unlock();
            }
        };
    }
}
