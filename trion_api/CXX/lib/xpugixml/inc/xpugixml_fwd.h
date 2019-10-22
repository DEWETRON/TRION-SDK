// Copyright (c) DEWETRON GmbH 2015
#pragma once

#if !defined(USE_BOOST) && !defined(USE_CXX17)
#error Select USE_CXX17 or USE_BOOST
#endif

#ifdef USE_BOOST
#include <boost/shared_ptr.hpp>
#include <boost/weak_ptr.hpp>
#endif

#ifdef USE_CXX17
#include <memory>
#endif

namespace pugi
{
    class xml_document;

#ifdef USE_BOOST
    typedef boost::shared_ptr<xml_document> xml_document_ptr;
    typedef boost::weak_ptr<xml_document> xml_document_wptr;
#endif

#ifdef USE_CXX17
    typedef std::shared_ptr<xml_document> xml_document_ptr;
    typedef std::weak_ptr<xml_document> xml_document_wptr;
#endif

} //pugi

namespace xpugi
{
    using xml_document_ptr = pugi::xml_document_ptr;
    using xml_document_wptr = pugi::xml_document_wptr;
} //xpugi
