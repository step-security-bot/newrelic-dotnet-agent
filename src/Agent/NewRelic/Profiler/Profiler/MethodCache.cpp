#pragma once

#include "stdafx.h"
#include "MethodCache.h"
#include "../MethodRewriter/IFunction.h"

bool NewRelic::Profiler::MethodCache::_initialized = false;
bool NewRelic::Profiler::MethodCache::_disabled = false;

std::mutex NewRelic::Profiler::MethodCache::_lock;

std::vector<ModuleID> NewRelic::Profiler::MethodCache::_modules;
std::vector<mdMethodDef> NewRelic::Profiler::MethodCache::_methods;
