/**
 * include/enet.h - a Single-Header auto-generated variant of enet.h library.
 *
 * Usage:
 * #define ENET_IMPLEMENTATION exactly in ONE source file right BEFORE including the library, like:
 *
 * #define ENET_IMPLEMENTATION
 * #include <enet.h>
 *
 * License:
 * The MIT License (MIT)
 *
 * Copyright (c) 2002-2016 Lee Salzman
 * Copyright (c) 2017-2021 Vladyslav Hrytsenko, Dominik Madarász
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */
#ifndef ENET_INCLUDE_H
#define ENET_INCLUDE_H

#include <assert.h>
#include <stdlib.h>
#include <stdbool.h>
#include <stdint.h>
#include <time.h>



// =======================================================================//
// !
// ! System differences
// !
// =======================================================================//

#if defined(_WIN32)
    #if defined(_MSC_VER) && defined(ENET_IMPLEMENTATION)
        #pragma warning (disable: 4267) // ulong to int conversion
        #pragma warning (disable: 4244) // 64bit to 32bit int
        #pragma warning (disable: 4018) // signed/unsigned mismatch
        #pragma warning (disable: 4146) // unary minus operator applied to unsigned type
    #endif

    #ifndef ENET_NO_PRAGMA_LINK
    #ifndef  __GNUC__
    #pragma comment(lib, "ws2_32.lib")
    #pragma comment(lib, "winmm.lib")
    #endif
    #endif

    #if _MSC_VER >= 1910
    /* It looks like there were changes as of Visual Studio 2017 and there are no 32/64 bit
       versions of _InterlockedExchange[operation], only InterlockedExchange[operation]
       (without leading underscore), so we have to distinguish between compiler versions */
    #define NOT_UNDERSCORED_INTERLOCKED_EXCHANGE
    #endif

    #ifdef __GNUC__
    #if (_WIN32_WINNT < 0x0600)
    #undef _WIN32_WINNT
    #define _WIN32_WINNT 0x0600
    #endif
    #endif

    #include <winsock2.h>
    #include <ws2tcpip.h>
    #include <mmsystem.h>
    #include <ws2ipdef.h>

    #include <intrin.h>

    #if defined(_WIN32) && defined(_MSC_VER)
    #if _MSC_VER < 1900
    typedef struct timespec {
        long tv_sec;
        long tv_nsec;
    };
    #endif
    #define CLOCK_MONOTONIC 0
    #endif

    typedef SOCKET Socket;
    #define null INVALID_SOCKET

    #define ENET_HOST_TO_NET_16(value) (htons(value))
    #define ENET_HOST_TO_NET_32(value) (htonl(value))

    #define ENET_NET_TO_HOST_16(value) (ntohs(value))
    #define ENET_NET_TO_HOST_32(value) (ntohl(value))

    typedef struct {
        ulong dataLength;
        void * data;
    } ENetBuffer;

    #define ENET_CALLBACK __cdecl

    #ifdef ENET_DLL
    #ifdef ENET_IMPLEMENTATION
    #define ENET_API __declspec( dllexport )
    #else
    #define ENET_API __declspec( dllimport )
    #endif // ENET_IMPLEMENTATION
    #else
    #define ENET_API extern
    #endif // ENET_DLL

    typedef fd_set ENetSocketSet;

    #define ENET_SOCKETSET_EMPTY(sockset)          FD_ZERO(&(sockset))
    #define ENET_SOCKETSET_ADD(sockset, socket)    FD_SET(socket, &(sockset))
    #define ENET_SOCKETSET_REMOVE(sockset, socket) FD_CLR(socket, &(sockset))
    #define ENET_SOCKETSET_CHECK(sockset, socket)  FD_ISSET(socket, &(sockset))
#endif



#define ENET_IPV6           1
static const struct in6_addr enet_v4_anyaddr   = {{{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00 }}};
static const struct in6_addr enet_v4_noaddr    = {{{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }}};
static const struct in6_addr enet_v4_localhost = {{{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0x7f, 0x00, 0x00, 0x01 }}};
static const struct in6_addr enet_v6_anyaddr   = {{{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }}};
static const struct in6_addr enet_v6_noaddr    = {{{ 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }}};
static const struct in6_addr enet_v6_localhost = {{{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }}};
#define ENET_HOST_ANY       in6addr_any
#define ENET_HOST_BROADCAST 0xFFFFFFFFU
#define ENET_PORT_ANY       0


// =======================================================================//
// !
// ! List
// !
// =======================================================================//

    typedef ENetListNode ENetListIterator;


// =======================================================================//
// !
// ! General ENet structs/enums
// !
// =======================================================================//



    #define in6_equal(in6_addr_a, in6_addr_b) (memcmp(&in6_addr_a, &in6_addr_b, sizeof(struct in6_addr)) == 0)




#endif // _WIN32


#endif // ENET_IMPLEMENTATION
#endif // ENET_INCLUDE_H
