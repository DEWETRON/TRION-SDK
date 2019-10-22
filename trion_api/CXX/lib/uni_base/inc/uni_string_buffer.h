// Copyright DEWETRON GmbH 2012
#pragma once

#ifndef _UNI_STRING_BUFFER_H_
#define _UNI_STRING_BUFFER_H_

// prevent min/max macro definitions from MSVC
#ifndef NOMINMAX
#  define NOMINMAX
#endif

#include "uni_stdio.h"

#ifdef USE_BOOST
#include <boost/static_assert.hpp>
#endif

#include <algorithm>
#include <iostream>
#include <stdarg.h>
#include <string>
#include <string.h>

namespace uni
{
    /**
     * The class StringBuffer is used for fast creation of formatted strings.
     */
    template <std::size_t MIN_SIZE, std::size_t MAX_SIZE = 1024, class CT = char>
    class StringBuffer
    {
    public:
        StringBuffer()
            : m_buffer(m_base_buffer)
            , m_length(0)
            , m_buffer_size(MIN_SIZE)
        {
#ifdef USE_BOOST
            BOOST_STATIC_ASSERT((MIN_SIZE <= MAX_SIZE) || (MAX_SIZE == 0));
#endif
            m_buffer[0] = 0;
        }

        ~StringBuffer()
        {
            if (m_buffer!=m_base_buffer)
            {
                free(m_buffer);
            }
        }

        void clear()
        {
            m_buffer[0] = 0;
            m_length = 0;
        }

        const CT* c_str() const
        {
            return m_buffer;
        }

        size_t length() const
        {
            return m_length;
        }

        std::string str() const
        {
            return std::string(m_buffer, m_length);
        }

        StringBuffer& operator+=(const CT* str)
        {
            size_t str_len = strlen(str);
            append(str, str_len);
            return *this;
        }

        StringBuffer& operator+=(const std::string& str)
        {
            append(str.c_str(), str.size());
            return *this;
        }

        StringBuffer& append(const std::string& str)
        {
            append(str.c_str(), str.size());
            return *this;
        }

        void printf(const CT* format, ...)
        {
            int needed;
            size_t available = m_buffer_size-m_length;
            bool enlarged = false;

            va_list ap;
            va_start(ap, format);
            needed = vsnprintf(m_buffer+m_length, available, format, ap);
            va_end(ap);

            while (MIN_SIZE!=MAX_SIZE && (size_t)needed+1>available)
            {
                if (!Enlarge())
                {
                    break;
                }
                enlarged = true;
                available = m_buffer_size-m_length;
            }
            if (enlarged)
            {
                va_start(ap, format);
                needed = vsnprintf(m_buffer+m_length, available, format, ap);
                va_end(ap);
            }
            m_length += std::min<size_t>(available-1, needed);
        }

        void replace(size_t start, size_t end, const CT& old_value, const CT& new_value)
        {
            if (old_value == new_value)
            {
                return;
            }

            if (start >= m_length)
            {
                return;
            }

            if (end >= m_length)
            {
                end = m_length - 1;
            }

            for (; start <= end; ++start)
            {
                if (m_buffer[start] == old_value)
                {
                    m_buffer[start] = new_value;
                }
            }
        }

        void replace(const CT& old_value, const CT& new_value)
        {
            replace(0, m_length - 1, old_value, new_value);
        }

    private:
        void append(const CT* str, std::size_t str_len)
        {
            size_t to_copy = m_buffer_size - m_length - 1;
            while (MIN_SIZE != MAX_SIZE && str_len >= to_copy)
            {
                if (!Enlarge())
                {
                    break;
                }
                to_copy = m_buffer_size - m_length - 1;
            }
            to_copy = std::min(to_copy, str_len);

            if (to_copy>0)
            {
                strncpy(m_buffer + m_length, str, to_copy);
                m_length += to_copy;
                m_buffer[m_length] = 0;
            }
        }

        bool Enlarge()
        {
            if (MIN_SIZE==MAX_SIZE) //shortcut for optimizer
            {
                return false;
            }
            if (MAX_SIZE==0 || m_buffer_size<MAX_SIZE)
            {
                size_t new_size = m_buffer_size*2;
                if (MAX_SIZE!=0 && new_size>MAX_SIZE)
                {
                    new_size = MAX_SIZE;
                }

                void* new_buffer;
                if (m_buffer!=m_base_buffer)
                {
                    new_buffer = realloc(m_buffer, new_size);
                }
                else
                {
                    new_buffer = malloc(new_size);
                    if (new_buffer)
                    {
                        memcpy(new_buffer, m_buffer, m_length + 1);
                    }
                }
                if (new_buffer)
                {
                    m_buffer = reinterpret_cast<CT*>(new_buffer);
                    m_buffer_size = new_size;
                    return true;
                }
            }
            return false;
        }

    private:
        CT m_base_buffer[MIN_SIZE];
        CT* m_buffer;
        size_t m_length;
        size_t m_buffer_size;
    };

} // end namespace uni

#endif //_UNI_STRING_BUFFER_H_
