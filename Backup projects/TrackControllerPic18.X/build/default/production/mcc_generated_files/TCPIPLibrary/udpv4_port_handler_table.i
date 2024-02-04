# 1 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c"
# 1 "<built-in>" 1
# 1 "<built-in>" 3
# 288 "<built-in>" 3
# 1 "<command line>" 1
# 1 "<built-in>" 2
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\language_support.h" 1 3
# 2 "<built-in>" 2
# 1 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c" 2
# 40 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c"
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdio.h" 1 3



# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\musl_xc8.h" 1 3
# 5 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdio.h" 2 3





# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\features.h" 1 3
# 11 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdio.h" 2 3
# 24 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdio.h" 3
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 1 3
# 12 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef void * va_list[1];




typedef void * __isoc_va_list[1];
# 128 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef unsigned size_t;
# 143 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef __int24 ssize_t;
# 174 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef __int24 int24_t;
# 210 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef __uint24 uint24_t;
# 255 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef long long off_t;
# 409 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef struct _IO_FILE FILE;
# 25 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdio.h" 2 3
# 52 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdio.h" 3
typedef union _G_fpos64_t {
 char __opaque[16];
 double __align;
} fpos_t;

extern FILE *const stdin;
extern FILE *const stdout;
extern FILE *const stderr;





FILE *fopen(const char *restrict, const char *restrict);
FILE *freopen(const char *restrict, const char *restrict, FILE *restrict);
int fclose(FILE *);

int remove(const char *);
int rename(const char *, const char *);

int feof(FILE *);
int ferror(FILE *);
int fflush(FILE *);
void clearerr(FILE *);

int fseek(FILE *, long, int);
long ftell(FILE *);
void rewind(FILE *);

int fgetpos(FILE *restrict, fpos_t *restrict);
int fsetpos(FILE *, const fpos_t *);

size_t fread(void *restrict, size_t, size_t, FILE *restrict);
size_t fwrite(const void *restrict, size_t, size_t, FILE *restrict);

int fgetc(FILE *);
int getc(FILE *);
int getchar(void);





int ungetc(int, FILE *);
int getch(void);

int fputc(int, FILE *);
int putc(int, FILE *);
int putchar(int);





void putch(char);

char *fgets(char *restrict, int, FILE *restrict);

char *gets(char *);


int fputs(const char *restrict, FILE *restrict);
int puts(const char *);

__attribute__((__format__(__printf__, 1, 2)))
int printf(const char *restrict, ...);
__attribute__((__format__(__printf__, 2, 3)))
int fprintf(FILE *restrict, const char *restrict, ...);
__attribute__((__format__(__printf__, 2, 3)))
int sprintf(char *restrict, const char *restrict, ...);
__attribute__((__format__(__printf__, 3, 4)))
int snprintf(char *restrict, size_t, const char *restrict, ...);

__attribute__((__format__(__printf__, 1, 0)))
int vprintf(const char *restrict, __isoc_va_list);
int vfprintf(FILE *restrict, const char *restrict, __isoc_va_list);
__attribute__((__format__(__printf__, 2, 0)))
int vsprintf(char *restrict, const char *restrict, __isoc_va_list);
__attribute__((__format__(__printf__, 3, 0)))
int vsnprintf(char *restrict, size_t, const char *restrict, __isoc_va_list);

__attribute__((__format__(__scanf__, 1, 2)))
int scanf(const char *restrict, ...);
__attribute__((__format__(__scanf__, 2, 3)))
int fscanf(FILE *restrict, const char *restrict, ...);
__attribute__((__format__(__scanf__, 2, 3)))
int sscanf(const char *restrict, const char *restrict, ...);

__attribute__((__format__(__scanf__, 1, 0)))
int vscanf(const char *restrict, __isoc_va_list);
int vfscanf(FILE *restrict, const char *restrict, __isoc_va_list);
__attribute__((__format__(__scanf__, 2, 0)))
int vsscanf(const char *restrict, const char *restrict, __isoc_va_list);

void perror(const char *);

int setvbuf(FILE *restrict, char *restrict, int, size_t);
void setbuf(FILE *restrict, char *restrict);

char *tmpnam(char *);
FILE *tmpfile(void);




FILE *fmemopen(void *restrict, size_t, const char *restrict);
FILE *open_memstream(char **, size_t *);
FILE *fdopen(int, const char *);
FILE *popen(const char *, const char *);
int pclose(FILE *);
int fileno(FILE *);
int fseeko(FILE *, off_t, int);
off_t ftello(FILE *);
int dprintf(int, const char *restrict, ...);
int vdprintf(int, const char *restrict, __isoc_va_list);
void flockfile(FILE *);
int ftrylockfile(FILE *);
void funlockfile(FILE *);
int getc_unlocked(FILE *);
int getchar_unlocked(void);
int putc_unlocked(int, FILE *);
int putchar_unlocked(int);
ssize_t getdelim(char **restrict, size_t *restrict, int, FILE *restrict);
ssize_t getline(char **restrict, size_t *restrict, FILE *restrict);
int renameat(int, const char *, int, const char *);
char *ctermid(char *);







char *tempnam(const char *, const char *);
# 41 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c" 2
# 1 "mcc_generated_files/TCPIPLibrary/tcpip_config.h" 1
# 42 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c" 2
# 1 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.h" 1
# 44 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.h"
# 1 "mcc_generated_files/TCPIPLibrary/tcpip_types.h" 1
# 47 "mcc_generated_files/TCPIPLibrary/tcpip_types.h"
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdint.h" 1 3
# 26 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdint.h" 3
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 1 3
# 133 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef unsigned __int24 uintptr_t;
# 148 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef __int24 intptr_t;
# 164 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef signed char int8_t;




typedef short int16_t;
# 179 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef long int32_t;





typedef long long int64_t;
# 194 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef long long intmax_t;





typedef unsigned char uint8_t;




typedef unsigned short uint16_t;
# 215 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef unsigned long uint32_t;





typedef unsigned long long uint64_t;
# 235 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/alltypes.h" 3
typedef unsigned long long uintmax_t;
# 27 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdint.h" 2 3

typedef int8_t int_fast8_t;

typedef int64_t int_fast64_t;


typedef int8_t int_least8_t;
typedef int16_t int_least16_t;

typedef int24_t int_least24_t;
typedef int24_t int_fast24_t;

typedef int32_t int_least32_t;

typedef int64_t int_least64_t;


typedef uint8_t uint_fast8_t;

typedef uint64_t uint_fast64_t;


typedef uint8_t uint_least8_t;
typedef uint16_t uint_least16_t;

typedef uint24_t uint_least24_t;
typedef uint24_t uint_fast24_t;

typedef uint32_t uint_least32_t;

typedef uint64_t uint_least64_t;
# 148 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdint.h" 3
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\bits/stdint.h" 1 3
typedef int16_t int_fast16_t;
typedef int32_t int_fast32_t;
typedef uint16_t uint_fast16_t;
typedef uint32_t uint_fast32_t;
# 149 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdint.h" 2 3
# 48 "mcc_generated_files/TCPIPLibrary/tcpip_types.h" 2

typedef enum {TCB_ERROR = -1, TCB_NO_ERROR = 0} tcbError_t;

typedef struct
{
    uint8_t destinationMAC[6];
    uint8_t sourceMAC[6];
    union
    {
        uint16_t type;
        uint16_t length;
        uint16_t tpid;
    }id;




} ethernetFrame_t;
# 120 "mcc_generated_files/TCPIPLibrary/tcpip_types.h"
typedef struct
{
    unsigned ihl:4;
    unsigned version:4;
    unsigned ecn:2;
    unsigned dscp:6;
    uint16_t length;
    uint16_t identifcation;
    unsigned fragmentOffsetHigh:5;
    unsigned :1;
    unsigned dontFragment:1;
    unsigned moreFragments:1;
    uint8_t fragmentOffsetLow;
    uint8_t timeToLive;
    uint8_t protocol;
    uint16_t headerCksm;
    uint32_t srcIpAddress;
    uint32_t dstIpAddress;


} ipv4Header_t;


typedef struct
{
    uint32_t srcIpAddress;
    uint32_t dstIpAddress;
    uint8_t protocol;
    uint8_t z;
    uint16_t length;
} ipv4_pseudo_header_t;

typedef struct
{
    union
    {
        uint16_t typeCode;
        struct
        {
            uint8_t code;
            uint8_t type;
        };
    };
    uint16_t checksum;
} icmpHeader_t;



typedef enum
{
    ECHO_REPLY = 0x0000,

    DEST_NETWORK_UNREACHABLE = 0x0300,
    DEST_HOST_UNREACHABLE = 0x0301,
    DEST_PROTOCOL_UNREACHABLE = 0x0302,
    DEST_PORT_UNREACHABLE = 0x0303,
    FRAGMENTATION_REQUIRED = 0x0304,
    SOURCE_ROUTE_FAILED = 0x0305,
    DESTINATION_NETWORK_UNKNOWN = 0x0306,
    SOURCE_HOST_ISOLATED = 0x0307,
    NETWORK_ADMINISTRATIVELY_PROHIBITED = 0x0308,
    HOST_ADMINISTRATIVELY_PROHIBITED = 0x0309,
    NETWORK_UNREACHABLE_FOR_TOS = 0x030A,
    HOST_UNREACHABLE_FOR_TOS = 0x030B,
    COMMUNICATION_ADMINISTRATIVELY_PROHIBITED = 0x030C,
    HOST_PRECEDENCE_VIOLATION = 0x030D,
    PRECEDENCE_CUTOFF_IN_EFFECT = 0x030E,

    SOURCE_QUENCH = 0x0400,

    REDIRECT_DATAGRAM_FOR_THE_NETWORK = 0x0500,
    REDIRECT_DATAGRAM_FOR_THE_HOST = 0x0501,
    REDIRECT_DATAGRAM_FOR_THE_TOS_AND_NETWORK = 0x0502,
    REDIRECT_DATAGRAM_FOR_THE_TOS_AND_HOST = 0x0503,

    ALTERNATE_HOST_ADDRESS = 0x0600,

    ECHO_REQUEST = 0x0800,


    UNASSIGNED_ECHO_TYPE_CODE_REQUEST_1 = 0x082A,
    UNASSIGNED_ECHO_TYPE_CODE_REQUEST_2 = 0x08FC,

    ROUTER_ADVERTISEMENT = 0x0900,
    ROUTER_SOLICITATION = 0x0A00,
    TRACEROUTE = 0x3000
} icmpTypeCodes_t;

typedef struct
{
    uint16_t srcPort;
    uint16_t dstPort;
    uint16_t length;
    uint16_t checksum;
} udpHeader_t;

typedef struct
{
    uint16_t sourcePort;
    uint16_t destPort;
    uint32_t sequenceNumber;
    uint32_t ackNumber;
    union{
        uint8_t byte13;
        struct{
            uint8_t ns:1;
            uint8_t reserved:3;
            uint8_t dataOffset:4;
        };
    };

    union{
        uint8_t flags;
        struct{
            uint8_t fin:1;
            uint8_t syn:1;
            uint8_t rst:1;
            uint8_t psh:1;
            uint8_t ack:1;
            uint8_t urg:1;
            uint8_t ece:1;
            uint8_t cwr:1;
        };
    };

    uint16_t windowSize;
    uint16_t checksum;
    uint16_t urgentPtr;



} tcpHeader_t;



typedef enum
{
    HOPOPT_TCPIP = 0,
    ICMP_TCPIP = 1,
    IGMP_TCPIP = 2,
    GGP_TCPIP = 3,
    IPV4_TCPIP = 4,
    ST_TCPIP = 5,
    TCP_TCPIP = 6,
    CBT_TCPIP = 7,
    EGP_TCPIP = 8,
    IGP_TCPIP = 9,
    BBN_RCC_MON_TCPIP = 10,
    NVP_II_TCPIP = 11,
    PUP_TCPIP = 12,
    ARGUS_TCPIP = 13,
    EMCON_TCPIP = 14,
    XNET_TCPIP = 15,
    CHAOS_TCPIP = 16,
    UDP_TCPIP = 17,
    MUX_TCPIP = 18,
    DCN_MEAS_TCPIP = 19,
    HMP_TCPIP = 20,
    PRM_TCPIP = 21,
    XNS_IDP_TCPIP = 22,
    TRUNK_1_TCPIP = 23,
    TRUNK_2_TCPIP = 24,
    LEAF_1_TCPIP = 25,
    LEAF_2_TCPIP = 26,
    RDP_TCPIP = 27,
    IRTP_TCPIP = 28,
    ISO_TP4_TCPIP = 29,
    NETBLT_TCPIP = 30,
    MFE_NSP_TCPIP = 31,
    MERIT_INP_TCPIP = 32,
    DCCP_TCPIP = 33,
    THREEPC_TCPIP = 34,
    IDPR_TCPIP = 35,
    XTP_TCPIP = 36,
    DDP_TCPIP = 37,
    IDPR_CMTP_TCPIP = 38,
    TPpp_TCPIP = 39,
    IL_TCPIP = 40,
    IPV6_TUNNEL_TCPIP = 41,
    SDRP_TCPIP = 42,
    IPV6_Route_TCPIP = 43,
    IPV6_Frag_TCPIP = 44,
    IDRP_TCPIP = 45,
    RSVP_TCPIP = 46,
    GRE_TCPIP = 47,
    DSR_TCPIP = 48,
    BNA_TCPIP = 49,
    ESP_TCPIP = 50,
    AH_TCPIP = 51,
    I_NLSP_TCPIP = 52,
    SWIPE_TCPIP = 53,
    NARP_TCPIP = 54,
    MOBILE_TCPIP = 55,
    TLSP_TCPIP = 56,
    SKIP_TCPIP = 57,
    IPV6_ICMP_TCPIP = 58,
    IPV6_NoNxt_TCPIP = 59,
    IPV6_Opts_TCPIP = 60,
    CFTP_TCPIP = 62,
    SAT_EXPAK_TCPIP = 64,
    KRYPTOLAN_TCPIP = 65,
    RVD_TCPIP = 66,
    IPPC_TCPIP = 67,
    SAT_MON_TCPIP = 69,
    VISA_TCPIP = 70,
    IPCV_TCPIP = 71,
    CPNX_TCPIP = 72,
    CPHB_TCPIP = 73,
    WSN_TCPIP = 74,
    PVP_TCPIP = 75,
    BR_SAT_MON_TCPIP = 76,
    SUN_ND_TCPIP = 77,
    WB_MON_TCPIP = 78,
    WB_EXPAK_TCPIP = 79,
    ISO_IP_TCPIP = 80,
    VMTP_TCPIP = 81,
    SECURE_VMTP_TCPIP = 82,
    VINES_TCPIP = 83,
    TTP_TCPIP = 84,
    IPTM_TCPIP = 84,
    NSFNET_IGP_TCPIP = 85,
    DGP_TCPIP = 86,
    TCF_TCPIP = 87,
    EIGRP_TCPIP = 88,
    OSPFIGP_TCPIP = 89,
    Sprite_RPC_TCPIP = 90,
    LARP_TCPIP = 91,
    MTP_TCPIP = 92,
    AX25_TCPIP = 93,
    IPIP_TCPIP = 94,
    MICP_TCPIP = 95,
    SCC_SP_TCPIP = 96,
    ETHERIP_TCPIP = 97,
    ENCAP_TCPIP = 98,
    GMTP_TCPIP = 100,
    IFMP_TCPIP = 101,
    PNNI_TCPIP = 102,
    PIM_TCPIP = 103,
    ARIS_TCPIP = 104,
    SCPS_TCPIP = 105,
    QNX_TCPIP = 106,
    A_N_TCPIP = 107,
    IPComp_TCPIP = 108,
    SNP_TCPIP = 109,
    Compaq_Peer_TCPIP = 110,
    IPX_in_IP_TCPIP = 111,
    VRRP_TCPIP = 112,
    PGM_TCPIP = 113,
    L2TP_TCPIP = 115,
    DDX_TCPIP = 116,
    IATP_TCPIP = 117,
    STP_TCPIP = 118,
    SRP_TCPIP = 119,
    UTI_TCPIP = 120,
    SMP_TCPIP = 121,
    SM_TCPIP = 122,
    PTP_TCPIP = 123,
    ISIS_TCPIP = 124,
    FIRE_TCPIP = 125,
    CRTP_TCPIP = 126,
    CRUDP_TCPIP = 127,
    SSCOPMCE_TCPIP = 128,
    IPLT_TCPIP = 129,
    SPS_TCPIP = 130,
    PIPE_TCPIP = 131,
    SCTP_TCPIP = 132,
    FC_TCPIP = 133
} ipProtocolNumbers;

typedef struct
{
    union{
        uint32_t s_addr;
        uint8_t s_addr_byte[4];
    };
}inAddr_t;


typedef struct
{
    uint16_t port;
    inAddr_t addr;
}sockaddr_in4_t;


extern const char *network_errors[];

typedef enum
{
    ERROR =0,
    SUCCESS,
    LINK_NOT_FOUND,
    BUFFER_BUSY,
    TX_LOGIC_NOT_IDLE,
    MAC_NOT_FOUND,
    IP_WRONG_VERSION,
    IPV4_CHECKSUM_FAILS,
    DEST_IP_NOT_MATCHED,
    ICMP_CHECKSUM_FAILS,
    UDP_CHECKSUM_FAILS,
    TCP_CHECKSUM_FAILS,
    DMA_TIMEOUT,
    PORT_NOT_AVAILABLE,
    ARP_IP_NOT_MATCHED,
    EAPoL_PACKET_FAILURE,
    INCORRECT_IPV4_HLEN,
    IPV4_NO_OPTIONS,
    TX_QUEUED,
    IPV6_CHECKSUM_FAILS,
    IPV6_LOCAL_ADDR_RESOLVE,
    IPV6_LOCAL_ADDR_INVALID,
    NO_GATEWAY,
    ADDRESS_RESOLUTION,
    GLOBAL_DESTINATION,
    ARP_WRONG_HARDWARE_ADDR_TYPE,
    ARP_WRONG_PROTOCOL_TYPE,
    ARP_WRONG_HARDWARE_ADDR_LEN,
    ARP_WRONG_PROTOCOL_LEN
}error_msg;

typedef struct
{
    inAddr_t dest_addr;
}destIP_t;


typedef int8_t socklistsize_t;

typedef void (*ip_receive_function_ptr)(int16_t);
# 45 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.h" 2

typedef struct
{
    uint16_t portNumber;
    ip_receive_function_ptr callBack;
} udp_handler_t;

typedef udp_handler_t * udp_table_iterator_t;

udp_table_iterator_t udp_table_getIterator(void);
udp_table_iterator_t udp_table_nextEntry(udp_table_iterator_t i);
# 43 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c" 2
# 1 "mcc_generated_files/TCPIPLibrary/../../communication.h" 1
# 15 "mcc_generated_files/TCPIPLibrary/../../communication.h"
# 1 "C:\\Program Files\\Microchip\\xc8\\v2.46\\pic\\include\\c99\\stdbool.h" 1 3
# 16 "mcc_generated_files/TCPIPLibrary/../../communication.h" 2
# 1 "./enums.h" 1
# 18 "./enums.h"
    const uint32_t tFactorSec = 1000;

    const uint8_t tRandomShift = 3;

    const uint32_t tSwitchPointWaitTime = (uint32_t)(7 * tFactorSec);

    const uint32_t tTrainWaitTime = (uint32_t)(100 * tFactorSec);

    const uint32_t tFreightTrainWaitTime = (uint32_t)(5 * tFactorSec);

    const uint32_t tReadIoSignalWaitTime = (uint32_t)(10 * tFactorSec);

    const uint32_t tOutboundWaitTime = (uint32_t)(10 * tFactorSec);

    const uint32_t tInOutboundStopWaitTime = (uint32_t)(10 * tFactorSec);

    typedef struct
    {
        volatile unsigned char *portx_ptr;
        uint8_t pin_mask;
    }OCC, REL;

    typedef struct
    {
        volatile unsigned char *port1_ptr;
        uint8_t pin1_mask;
        volatile unsigned char *port2_ptr;
        uint8_t pin2_mask;
        volatile unsigned char *port3_ptr;
        uint8_t pin3_mask;
        volatile unsigned char *port4_ptr;
        uint8_t pin4_mask;
        volatile unsigned char *port5_ptr;
        uint8_t pin5_mask;
        volatile unsigned char *port6_ptr;
        uint8_t pin6_mask;

    }WS, SIG;

    typedef enum
    {
        MAIN_LOOP = 01,
        MAIN_STATION_TOP = 10,
        MAIN_STATION_BOT = 20,
        WALDSEE = 30,
        SIEBWALDE = 40,
        WALDBERG = 50,
        IODATA = 60,
    } TASK_ID;

    typedef enum
    {
        HEADER = 0xAA,
        ALIVE = 0xFF,
        STNTRACK1 = 81,
        STNTRACK2 = 82,
        STNTRACK3 = 83,
        MTNTRACK1 = 84,
        MTNTRACK2 = 85,
        MTNTRACK7 = 86,
        MTNTRACK8 = 87,
  VOLTAGE_DETECTED = 88,

    } TASK_COMMAND;

    typedef enum
    {
        busy = 100,
        connected = 101,
        done = 102,
        command = 103,
        nop = 104,
  INIT = 105,
        INIT2 = 106,
        RUN = 107,
        WAIT = 108,
        IDLE = 109,
  STN_TO_SIEBWALDE = 110,
  SIEBWALDE_TO_STN = 111,
        SIEBWALDE_SWITCHT4T5_OUT = 112,
        SIEBWALDE_SWITCHT4T5_IN = 113,
        SIEBWALDE_SWITCHT4T5 = 114,
        SIEWALDE_SET_NEXT = 115,
        STN_INBOUND = 116,
        STN_OUTBOUND = 117,
  STN_PASSING = 118,
        STN_WAIT = 119,
        STN_IDLE = 120,
        SEQ_IDLE = 121,
        SEQ_WAIT = 122,
        SEQ_SET_OCC = 123,
  SEQ_CHK_TRAIN = 124,
        SEQ_CHK_PASSED = 125,
        SEQ_OUTBOUND_LEFT_STATTION = 126,
        SEQ_INBOUND_BRAKE_TIME = 127,
        SEQ_OUTBOUND_BRAKE_TIME = 128,
        SIG_RED = 129,
        SIG_GREEN = 130,
        INVERT = 131,
        TIME = 132,
        SET_PATH_WAY = 133,
        SET_SIGNAL = 134,
    } TASK_STATE;

    typedef enum
    {
       NONE = 150,
       T1 = 151,
       T2 = 152,
       T3 = 153,
       T4 = 154,
       T5 = 155,
       T7 = 157,
       T8 = 158,
       TRACK1 = 159,
       TRACK2 = 160,
       TRACK3 = 161,
       TRACK10 = 162,
       TRACK11 = 163,
       TRACK12 = 164,

    } TASK_MESSAGES;
# 17 "mcc_generated_files/TCPIPLibrary/../../communication.h" 2




    typedef struct
    {
        uint32_t destinationAddress;
        uint16_t destinationPortNumber;
        uint32_t sourceAddress;
        uint16_t sourcePortNumber;
    }udpStart_t;

    typedef struct
    {
        uint8_t header;
        uint8_t command;
        uint8_t data[8];
    }udpTrans_t;

    const uint8_t udpTrans_t_length = (sizeof(udpTrans_t) / sizeof(uint8_t));
    const uint8_t udpTrans_t_data_length = udpTrans_t_length - 2;

    typedef enum
    {

        ETHERNET_LINKUP,
        ETHERNET_CONNECT,
        ETHERNET_NTP,
        ETHERNET_DATA_RX,
        ETHERNET_DATA_TX,
        ETHERNET_TCPIP_ERROR,



    } ETH_STATES;

    void COMMxSETxRXxDATAxHANDLER(void (* RxDataHandler)(void));
    extern void (*DataRxHandler)(void);

    extern void PROCESSxETHxDATAxINIT(void);
    extern void UDPxDATAxRECV(int16_t length);
    extern void PROCESSxETHxDATA(void);

    extern udpTrans_t *GETxDATAxFROMxRECEIVExMAILxBOX(void);
    extern void PUTxDATAxINxSENDxMAILxBOX (udpTrans_t *data);
    extern _Bool CHECKxDATAxINxRECEIVExMAILxBOX(void);
    extern void CREATExTASKxSTATUSxMESSAGE(
                            uint8_t task_id,
                            uint8_t task_command,
                            uint8_t task_state,
                            uint8_t task_messages);
    extern _Bool isUdpConnected;
    extern void CREATExDATAxMESSAGE(
                            uint8_t task_id,
                            uint8_t task_command,
                            uint8_t task_state,
                            uint8_t *data);
# 44 "mcc_generated_files/TCPIPLibrary/udpv4_port_handler_table.c" 2

const udp_handler_t UDP_CallBackTable[] = {

 {60000 , UDPxDATAxRECV},
};



udp_table_iterator_t udp_table_getIterator(void)
{
    return (udp_table_iterator_t) UDP_CallBackTable;
}

udp_table_iterator_t udp_table_nextEntry(udp_table_iterator_t i)
{
    i ++;
    if(i < UDP_CallBackTable + sizeof(UDP_CallBackTable))
    {
        return (udp_table_iterator_t) i;
    }
    else
        return (udp_table_iterator_t) ((void*)0);
}
