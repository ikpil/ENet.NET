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


#define ENET_TIME_OVERFLOW 86400000
#define ENET_TIME_LESS(a, b) ((a) - (b) >= ENET_TIME_OVERFLOW)
#define ENET_TIME_GREATER(a, b) ((b) - (a) >= ENET_TIME_OVERFLOW)
#define ENET_TIME_LESS_EQUAL(a, b) (! ENET_TIME_GREATER (a, b))
#define ENET_TIME_GREATER_EQUAL(a, b) (! ENET_TIME_LESS (a, b))
#define ENET_TIME_DIFFERENCE(a, b) ((a) - (b) >= ENET_TIME_OVERFLOW ? (b) - (a) : (a) - (b))

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

    typedef SOCKET ENetSocket;
    #define ENET_SOCKET_NULL INVALID_SOCKET

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
#else
    #include <sys/types.h>
    #include <sys/ioctl.h>
    #include <sys/time.h>
    #include <sys/socket.h>
    #include <poll.h>
    #include <arpa/inet.h>
    #include <netinet/in.h>
    #include <netinet/tcp.h>
    #include <netdb.h>
    #include <unistd.h>
    #include <string.h>
    #include <errno.h>
    #include <fcntl.h>

    #ifdef __APPLE__
    #include <mach/clock.h>
    #include <mach/mach.h>
    #include <Availability.h>
    #endif

    #ifndef MSG_NOSIGNAL
    #define MSG_NOSIGNAL 0
    #endif

    #ifdef MSG_MAXIOVLEN
    #define ENET_BUFFER_MAXIMUM MSG_MAXIOVLEN
    #endif

    typedef int ENetSocket;

    #define ENET_SOCKET_NULL -1

    #define ENET_HOST_TO_NET_16(value) (htons(value)) /* macro that converts host to net byte-order of a 16-bit value */
    #define ENET_HOST_TO_NET_32(value) (htonl(value)) /* macro that converts host to net byte-order of a 32-bit value */

    #define ENET_NET_TO_HOST_16(value) (ntohs(value)) /* macro that converts net to host byte-order of a 16-bit value */
    #define ENET_NET_TO_HOST_32(value) (ntohl(value)) /* macro that converts net to host byte-order of a 32-bit value */

    #define ENET_CALLBACK
    #define ENET_API extern

    typedef fd_set ENetSocketSet;

    #define ENET_SOCKETSET_EMPTY(sockset)          FD_ZERO(&(sockset))
    #define ENET_SOCKETSET_ADD(sockset, socket)    FD_SET(socket, &(sockset))
    #define ENET_SOCKETSET_REMOVE(sockset, socket) FD_CLR(socket, &(sockset))
    #define ENET_SOCKETSET_CHECK(sockset, socket)  FD_ISSET(socket, &(sockset))
#endif

#ifdef __GNUC__
#define ENET_DEPRECATED(func) func __attribute__ ((deprecated))
#elif defined(_MSC_VER)
#define ENET_DEPRECATED(func) __declspec(deprecated) func
#else
#pragma message("WARNING: Please ENET_DEPRECATED for this compiler")
#define ENET_DEPRECATED(func) func
#endif

#ifndef ENET_BUFFER_MAXIMUM
#define ENET_BUFFER_MAXIMUM (1 + 2 * ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS)
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


    /** An ENet packet compressor for compressing UDP packets before socket sends or receives. */
    typedef struct _ENetCompressor {
        /** Context data for the compressor. Must be non-NULL. */
        void *context;

        /** Compresses from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure. */
        ulong(ENET_CALLBACK * compress) (void *context, const ENetBuffer * inBuffers, ulong inBufferCount, ulong inLimit, byte * outData, ulong outLimit);

        /** Decompresses from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure. */
        ulong(ENET_CALLBACK * decompress) (void *context, const byte * inData, ulong inLimit, byte * outData, ulong outLimit);

        /** Destroys the context when compression is disabled or the host is destroyed. May be NULL. */
        void (ENET_CALLBACK * destroy)(void *context);
    } ENetCompressor;

    /** Callback that computes the checksum of the data held in buffers[0:bufferCount-1] */
    typedef uint (ENET_CALLBACK * ENetChecksumCallback)(const ENetBuffer *buffers, ulong bufferCount);

    /** Callback for intercepting received raw UDP packets. Should return 1 to intercept, 0 to ignore, or -1 to propagate an error. */
    typedef int (ENET_CALLBACK * ENetInterceptCallback)(struct _ENetHost *host, void *event);



#endif // _WIN32


#endif // ENET_IMPLEMENTATION
#endif // ENET_INCLUDE_H
