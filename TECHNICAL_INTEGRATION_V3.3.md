# Aaple Sarkar Portal Technical Integration Document

**Version:** 3.3
**Document Type:** Technical Integration Specification
**Prepared By:** Maharashtra Information Technology Corporation (MahaIT)
**Authors:** Suresh Rasal & Sudhir Chavan
**Updated By:** Ashwini Bhoite
**Release Date:** 01 June 2018

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Right To Services (RTS) Integration Architecture](#2-right-to-services-rts-integration-architecture)
3. [Step-wise Detailed Technical Process](#3-step-wise-detailed-technical-process)
   - [3.1 Step 4: Call pushWebService()](#31-step-4-call-pushwebservice-web-service-from-department)
   - [3.2 Step 11: Call pullWebService()](#32-step-11-call-pullwebservice-web-service-from-department)
   - [3.3 Step 3: Payment POST Data](#33-step-3-payment-post-data-from-department)

---

## 1. Introduction

### Purpose

Right to Public Services legislation in India comprises statutory laws which guarantee time-bound delivery of services for various public services rendered by the Government to Citizens and provides mechanism for punishing the errant public servant who is deficient in providing the service stipulated under the statute.

**Maharashtra Govt. passed Right to Public Service Ordinance on 28th April 2015.**

### Document Purpose

This document outlines the proposed system technical integration design for departmental notified services integration with Aaple Sarkar Portal. It describes in sufficient detail how the system is to be designed and developed, identifying the high-level system architecture.

---

## 2. Right To Services (RTS) Integration Architecture

### Overview

The integration consists of 14 steps divided between:
- **Aaple Sarkar Portal (MahaOnline) Side** - Central platform
- **Department Side** - Individual government departments

### 2.1 Step 1: Registration of Client, Department & Notified Service

**One-time master data repository process**

MahaIT provides checklist for registration. Departments need to fill provided checklist in English and Marathi.

#### Client Code Registration

| Field | Description | Example |
|-------|-------------|---------|
| Type of Client | Department/Corporation | Department/Corporation |
| Client Name (English) | Name of Client in English | - |
| Client Name (Marathi) | Name of Client in Marathi | - |
| Client Postal Address | Detail postal address with PinCode, District & Taluka | - |
| Mobile/Telephone | Contact details | - |
| Email Id | Email address for communication | - |
| Domain Name | - | www.kdmc.gov.in |
| IP Address | - | 103.234.23.30 |

#### Department Code Registration

Similar structure with department-specific details.

#### Notified Service Registration

| Field | Description |
|-------|-------------|
| Service Name (English) | Service name in English |
| Service Name (Marathi) | Service name in Marathi |
| No of Disposal periods | Number of days for disposal |
| Department Name | Parent department |
| Competent Authority | Responsible authority |
| First Appellant Officer | First level appellant |
| Second Appellant Officer | Second level appellant |

**Example:**
- Service: Birth Certificate
- Disposal Period: 3 days
- Department: Medical
- Competent Authority: Sub-Registrar/Senior Medical Officer/Medical Superintendent
- First Appellant: Additional Medical Officer
- Second Appellant: Health Medical Officer (MOH)

### 2.2 Step 2: Generation of Client Code, Department Code & Service ID

MahaIT generates unique identifiers (one-time activity):

| Parameter | Value | Description |
|-----------|-------|-------------|
| ClientCode | ************* | MahaIT-provided client code |
| EncryptKey | ************* | MahaIT-provided Encrypt Key |
| EncryptIV | ************* | MahaIT-provided Encrypt IV |
| ChecksumKey | ************* | MahaIT-provided Checksum Key |

### 2.3 Step 3: Departmental Service Enable at Aaple Sarkar Portal

MahaIT enables list of Department Name links at Aaple Sarkar Portal for end users.

### 2.4 Step 4: Call pushWebService() web-service from Department

Department consumes `pushWebService()` web-service (shared by MahaIT) for Authentication & Authorization to avoid fake requests.

### 2.5 Step 5: Validate Request at Aaple Sarkar Portal End

MahaIT validates departmental request for authentication and authorization.

### 2.6 Step 6: Response of pushWebService() web-service

MahaIT returns list of parameters (citizen register info) in XML format to Department.

### 2.7 Step 7: Business Process at Department End

Department processes received parameters and shows appropriate Notified Service Form.

### 2.8 Step 8: Display Service Form

Citizen fills the Form and submits request.

### 2.9 Step 9: Store Request data into Departmental Data Base

Department stores citizen request into departmental DB.

### 2.10 Step 10: Generate Track ID

Department generates Unique Application Identification Number (App Id).

### 2.11 Step 11: Call pullWebService() web-service from Department

Department consumes `pullWebService()` and provides parameters (String) to update status.

### 2.12 Step 12: Business Process at Aaple Sarkar Portal End

Aaple Sarkar Portal receives parameters and processes business logic.

### 2.13 Step 13: Store Response data into Aaple Sarkar Portal Data Base

Aaple Sarkar Portal stores departmental response in database.

### 2.14 Step 14: Acknowledge Message at Aaple Sarkar Portal End

Aaple Sarkar Portal/Department displays acknowledge message with Unique Departmental Application ID. User receives acknowledgment via SMS & Email.

---

## 3. Step-wise Detailed Technical Process

### 3.1 Step 4: Call pushWebService() web-service from Department

#### 3.1.1 Encryption Algorithm

**Triple Data Encryption Standard (TripleDES)**

TripleDES takes three 64-bit keys for an overall key length of 192 bits (24 characters).

**Required Parameters:**

1. **Initialization vector (IV)** - Provided by MahaIT
2. **Secret key** - Provided by MahaIT
3. **Mode** - CBC (Cipher Block Chaining)
   - .NET: `CipherMode.CBC`
   - Java: `"TripleDES/CBC/NoPadding"`
4. **Padding Mode**
   - .NET: `PaddingMode.Zeros`
   - Java: `NoPadding`
5. **Character Encoding** - UTF-8

#### 3.1.2 Encryption and Decryption Code

##### .NET Sample Code

**Encryption Function:**

```csharp
String SimpleTripleDes(String Data, string strKey, string striv)
{
    byte[] key = Encoding.UTF8.GetBytes(strKey);
    byte[] iv = Encoding.UTF8.GetBytes(striv);
    byte[] data = Encoding.UTF8.GetBytes(Data);
    byte[] enc = new byte[0];

    TripleDES tdes = TripleDES.Create();
    tdes.IV = iv;
    tdes.Key = key;
    tdes.Mode = CipherMode.CBC;
    tdes.Padding = PaddingMode.Zeros;

    ICryptoTransform ict = tdes.CreateEncryptor();
    enc = ict.TransformFinalBlock(data, 0, data.Length);

    return ByteArrayToString(enc);
}
```

**Decryption Function:**

```csharp
String SimpleTripleDesDecrypt(String Data, string strKey, string striv)
{
    byte[] key = Encoding.UTF8.GetBytes(strKey);
    byte[] iv = Encoding.UTF8.GetBytes(striv);
    byte[] data = StringToByteArray(Data);
    byte[] enc = new byte[0];

    TripleDES tdes = TripleDES.Create();
    tdes.IV = iv;
    tdes.Key = key;
    tdes.Mode = CipherMode.CBC;
    tdes.Padding = PaddingMode.Zeros;

    ICryptoTransform ict = tdes.CreateDecryptor();
    enc = ict.TransformFinalBlock(data, 0, data.Length);

    return Encoding.UTF8.GetString(enc).TrimEnd('\0');
}
```

**String to Byte Array:**

```csharp
byte[] StringToByteArray(String hex)
{
    int NumberChars = hex.Length;
    byte[] bytes = new byte[NumberChars / 2];

    for (int i = 0; i < NumberChars; i += 2)
        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

    return bytes;
}
```

**Byte Array to String:**

```csharp
string ByteArrayToString(byte[] ba)
{
    string hex = BitConverter.ToString(ba);
    return hex.Replace("-", "");
}
```

##### JAVA Sample Code

**Encryption Function:**

```java
String SimpleTripleDes(String Data, String strKey, String striv)
{
    Byte[] cipherBytes = StringToByteArray(Data);
    byte[] iv;
    Byte[] result = null;

    try {
        iv = striv.getBytes("UTF-8");
        byte[] keyBytes = strKey.getBytes("UTF-8");
        SecretKey aesKey = new SecretKeySpec(keyBytes, "TripleDES");
        Cipher cipher = Cipher.getInstance("TripleDES/CBC/Nopadding");
        cipher.init(Cipher.ENCRYPT_MODE, aesKey, new IvParameterSpec(iv));
        result = cipher.doFinal(cipherBytes);
    } catch (Exception e) {
        e.printStackTrace();
    }

    return ByteArrayToString(result);
}
```

**Decryption Function:**

```java
String SimpleTripleDesDecrypt(String Data, String strKey, String striv)
{
    Byte[] cipherBytes = StringToByteArray(Data);
    byte[] iv;
    Byte[] result = null;

    try {
        iv = striv.getBytes("UTF-8");
        byte[] keyBytes = strKey.getBytes("UTF-8");
        SecretKey aesKey = new SecretKeySpec(keyBytes, "TripleDES");
        Cipher cipher = Cipher.getInstance("TripleDES/CBC/Nopadding");
        cipher.init(Cipher.DECRYPT_MODE, aesKey, new IvParameterSpec(iv));
        result = cipher.doFinal(cipherBytes);
    } catch (Exception e) {
        e.printStackTrace();
    }

    return new String(result).trim();
}
```

#### 3.1.3 How to Decrypt Query String Token

```csharp
string str = Convert.ToString(Request.QueryString["str"]);
string RequestDecryStr = SimpleTripleDesDecrypt(str, ClientEncryptKey, ClientEncryptIV);
```

#### 3.1.4 Sample Format of Decrypted Token

```
UserId|TimeStamp|SessionID|CheckSumValue|AuthorizationToken
```

#### 3.1.5 Token Parameters

| Sr.No | Parameter | Value | Description |
|-------|-----------|-------|-------------|
| 1 | UserId | 59fbcf05-f9f7-47d9-ae90-742122c6a292 | 32 bit unique number of each user |
| 2 | TimeStamp | 5575676867789890 | Current Date Time |
| 3 | SessionID | Fgdfg654656466666rgfg | User Session id |
| 4 | CheckSumValue | 57789990 | Check sum values calculated at Aaple Sarkar Portal end |
| 5 | Authorization token / strServiceCookie | 45fbcf05-f9f7-47d9-ae90-742122c6a852 | Authorization token calculated at Aaple Sarkar Portal end |

#### 3.1.6 Checksum Validation

Checksum is a count of the number of bits in a transmission unit that is included with the unit so that the receiver can check whether the same number of bits arrived. If the counts match, it's assumed that the complete transmission was received.

**A Cyclic Redundancy Check (CRC) or Polynomial Code checksum** is a hash function designed to detect accidental changes to raw computer data.

##### .NET CRC32 Algorithm

```csharp
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Utility
{
    public class CRC32 : HashAlgorithm
    {
        public const UInt32 DefaultPolynomial = 0xedb88320;
        public const UInt32 DefaultSeed = 0xffffffff;

        private UInt32 hash;
        private UInt32 seed;
        private UInt32[] table;
        private static UInt32[] defaultTable;

        public CRC32()
        {
            table = InitializeTable(DefaultPolynomial);
            seed = DefaultSeed;
            Initialize();
        }

        public CRC32(UInt32 polynomial, UInt32 seed)
        {
            table = InitializeTable(polynomial);
            this.seed = seed;
            Initialize();
        }

        public override void Initialize()
        {
            hash = seed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {
            hash = CalculateHash(table, hash, buffer, start, length);
        }

        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = UInt32ToBigEndianBytes(~hash);
            this.HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize
        {
            get { return 32; }
        }

        public static UInt32 Compute(byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(DefaultPolynomial),
                DefaultSeed, buffer, 0, buffer.Length);
        }

        public static UInt32 Compute(UInt32 seed, byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(DefaultPolynomial),
                seed, buffer, 0, buffer.Length);
        }

        public static UInt32 Compute(UInt32 polynomial, UInt32 seed, byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(polynomial), seed, buffer,
                0, buffer.Length);
        }

        private static UInt32[] InitializeTable(UInt32 polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultTable != null)
                return defaultTable;

            UInt32[] createTable = new UInt32[256];
            for (int i = 0; i < 256; i++)
            {
                UInt32 entry = (UInt32)i;
                for (int j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                defaultTable = createTable;

            return createTable;
        }

        private static UInt32 CalculateHash(UInt32[] table, UInt32 seed, byte[]
            buffer, int start, int size)
        {
            UInt32 crc = seed;
            for (int i = start; i < size; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
                }
            return crc;
        }

        private byte[] UInt32ToBigEndianBytes(UInt32 x)
        {
            return new byte[] {
                (byte)((x >> 24) & 0xff),
                (byte)((x >> 16) & 0xff),
                (byte)((x >> 8) & 0xff),
                (byte)(x & 0xff)
            };
        }
    }
}
```

##### JAVA CRC32 Algorithm

```java
package org.com;

import java.util.zip.Checksum;

public class CRC32 implements Checksum
{
    /** The crc data checksum so far. */
    private int crc = 0;

    /** The fast CRC table. Computed once when the CRC32 class is loaded. */
    private static int[] crc_table = make_crc_table();

    /** Make the table for a fast CRC. */
    private static int[] make_crc_table()
    {
        int[] crc_table = new int[256];
        for (int n = 0; n < 256; n++)
        {
            int c = n;
            for (int k = 8; --k >= 0; )
            {
                if ((c & 1) != 0)
                    c = 0xedb88320 ^ (c >>> 1);
                else
                    c = c >>> 1;
            }
            crc_table[n] = c;
        }
        return crc_table;
    }

    /**
     * Returns the CRC32 data checksum computed so far.
     */
    public long getValue()
    {
        return (long) crc & 0xffffffffL;
    }

    /**
     * Resets the CRC32 data checksum as if no update was ever called.
     */
    public void reset() { crc = 0; }

    /**
     * Updates the checksum with the int bval.
     * @param bval (the byte is taken as the lower 8 bits of bval)
     */
    public void update(int bval)
    {
        int c = ~crc;
        c = crc_table[(c ^ bval) & 0xff] ^ (c >>> 8);
        crc = ~c;
    }

    /**
     * Adds the byte array to the data checksum.
     * @param buf the buffer which contains the data
     * @param off the offset in the buffer where the data starts
     * @param len the length of the data
     */
    public void update(byte[] buf, int off, int len)
    {
        int c = ~crc;
        while (--len >= 0)
            c = crc_table[(c ^ buf[off++]) & 0xff] ^ (c >>> 8);
        crc = ~c;
    }

    /**
     * Adds the complete byte array to the data checksum.
     */
    public void update(byte[] buf) { update(buf, 0, buf.length); }
}
```

#### 3.1.7 Steps to Calculate CheckSum Value and Validation

##### .NET Sample Code

```csharp
string[] param = RequestDecryStr.Split('|');
if (param != null && param.Length > 0)
{
    string _UsrId = string.Empty;
    string _UsrTimeStamp = string.Empty;
    string _UsrSession = string.Empty;
    string _ClientCheckSumValue = string.Empty;
    string _trackid = string.Empty;
    string _strServiceCookie = string.Empty; // New Addition
    string _ChkValueRawData = string.Empty;

    _UsrId = param[0];
    _UsrTimeStamp = param[1];
    _UsrSession = param[2];
    _ClientCheckSumValue = param[3];
    _strServiceCookie = param[4]; // New Addition

    _ChkValueRawData = String.Format("{0}|{1}|{2}|{3}|{4}",
        _UsrId, _UsrTimeStamp, _UsrSession, _ClientCheckSumValue,
        _strServiceCookie); // New Addition

    string _CaluculatedCheckSumValue =
        GenerateCheckSumValue(_ChkValueRawData);

    if (_ClientCheckSumValue.Equals(_CaluculatedCheckSumValue))
    {
        // Then here call the web service
    }
    else
    {
        // "Invalid checksum value";
    }
}
else
{
    // "Invalid Request param";
}
```

**Generate CheckSum Value:**

```csharp
public static string GenerateCheckSumValue(string reqStr)
{
    try
    {
        System.Text.ASCIIEncoding AsciiEncoding = new System.Text.ASCIIEncoding();
        UInt32 checksumvalue = CRC32.Compute(AsciiEncoding.GetBytes(reqStr));
        return checksumvalue.ToString();
    }
    catch (Exception)
    {
        return string.Empty;
    }
}
```

##### JAVA Sample Code

```java
String ReturnURL = "ClientRegsitrationReturnURL";
String ClientCode = "ClientCode";
String ClientEncryptKey = "ClientEncryptKey";
String ClientEncryptIV = "ClientEncryptIV";
String _CheckSumkey = "Checksum";
String CheckSumRequestValue = "", CheckSumRequestString = "",
       CalCheckSumValue = "", culture = "";

String param[] = decryptData.split("\\|");
String userId = param[0];
String userTimeStamp = param[1];
String userSession = param[2];
String clientCheckSumValue = param[3];
String strServiceCookie = param[4];
String checkSumkey = "GMahwp8v3G7M";

String checkValueRowData =
    userId+"|"+userTimeStamp+"|"+userSession+"|"+ checkSumkey+"|"+
    strServiceCookie;

ValidateCheckSum vchkSum = new ValidateCheckSum();
String caluculatedCheckSumValue =
    String.valueOf(vchkSum.CalulateCheckSum(checkValueRowData));

if (clientCheckSumValue.equals(caluculatedCheckSumValue))
{
    // Then here call the web service
}
else
{
    // "Invalid checksum value";
}
```

#### 3.1.8 Push Web-service Calling

**Testing URL:** http://testcitizenservices.MahaITgov.in/Dept_Authentication.asmx?WSDL

**Web method:**

```csharp
String xmlResponse = GetParameterNew(str, ClientCode); // New Addition
String ResponseXML = SimpleTripleDesDecrypt(xmlResponse, EncryptKey, EncryptIV);
```

**Request Parameters:**

| Sr | Parameter | Value | Description |
|----|-----------|-------|-------------|
| 1 | Str | 63458359jmfkklfllf8499090 | Token received from Aaple Sarkar Portal in query string |
| 2 | ClientCode | ********** | Provided by MahaIT |

**Response Parameters:**

| Sr No | Parameter Name | Value | Description |
|-------|----------------|-------|-------------|
| 1 | Response | Success/failed/Error | Status of request |
| 2 | UserID | "NNNN" | User Identification Number |
| 3 | UsertypeName | User | Type of User |
| 4 | Password | F91E15DBEC69FC40F81F0876E7009648 | Password in MD5 encrypted Format |
| 5 | PasswordChanged | True/false | Is Password changed? |
| 6 | IsActive | True/false | Is Active? |
| 7 | VerifyStatus | Verified/Not Verified | Is Status Verified or Not? |
| 8 | EmailID | suresh.rasal@MahaIT.gov.in | Email Id of User |
| 9 | MobileNo | 9888898888 | Mobile No of User |
| 10 | Salutation | MR/MISS/MRS | Salutation of User |
| 11 | FullName | Xxxxxxx | Full Name of User |
| 12 | FullName_mr | Xxxxxxx | Full Name in Marathi |
| 13 | Age | 30 | Age of User |
| 14 | Gender | M/F/T | Gender of User |
| 15 | UIDNO | 127890865443 | Aadhaar No of User |
| 16 | PANNo | ASXh4789N | PAN No of User |
| 17 | DOB | 14-12-1984 | Date of Birth of User |
| 18 | AddrCareOf | Xxxxxx | Address Care of User |
| 19 | AddrCareOf_LL | Xxxx | Address Care of User in Marathi |
| 20 | AddrBuilding | Xxxxxx | Building Address of User |
| 21 | AddrBuilding_LL | Xxxxxxx | Building Address of User in Marathi |
| 22 | AddrStreet | Xxxxxxx | Street Address of User |
| 23 | AddrStreet_LL | Xxxx | Street Address of User in Marathi |
| 24 | AddrLandmark | Xxx | Address Landmark |
| 25 | AddrLandmark_LL | Xxx | Address Landmark in Marathi |
| 26 | AddrLocality | Xxxxx | Address Locality |
| 27 | AddrLocality_LL | Xxxxxx | Address Locality in Marathi |
| 28 | PinCode | 445656 | Pin Code of User |
| 29 | DistrictID | 345 | District Code |
| 30 | TalukaID | 2345 | Taluka Code |
| 31 | VillageID | 23456 | Village Code |
| 32 | FatherFullName | Xxxxxx | Father Full Name |
| 33 | FatherFullName_mr | Xxxx | Father Full Name in Marathi |
| 34 | Father_Salutation | MR/MISS/MRS | Father Salutation |
| 35 | TrackId | 160115001100000001 | MahaIT Track ID |

**XML Response Example:**

```xml
<ResMessage>
    <Response>Success</Response>
    <UserID>59fbcf05-f9f7-47d9-ae90-742122c6a292</UserID>
    <UsertypeName>Super</UsertypeName>
    <Username>sadmin</Username>
    <Password>F91E15DBEC69FC40F81F0876E7009648</Password>
    <PasswordChanged>true</PasswordChanged>
    <IsActive>true</IsActive>
    <VerifyStatus>Verified</VerifyStatus>
    <EmailID>suresh.rasal@MahaIT.gov.in</EmailID>
    <MobileNo>9888898888</MobileNo>
    <Salutation>CA</Salutation>
    <FullName>Suresh Rasal</FullName>
    <FullName_mr></FullName_mr>
    <Age>29</Age>
    <Gender>M</Gender>
    <UIDNO>NA</UIDNO>
    <PANNo>NA</PANNo>
    <DOB>01/01/1988</DOB>
    <AddrCareOf>Bandra Mumbai</AddrCareOf>
    <AddrCareOf_LL></AddrCareOf_LL>
    <AddrBuilding>godrej</AddrBuilding>
    <AddrBuilding_LL></AddrBuilding_LL>
    <AddrStreet>godrej</AddrStreet>
    <AddrStreet_LL></AddrStreet_LL>
    <AddrLandmark>godrej</AddrLandmark>
    <AddrLandmark_LL></AddrLandmark_LL>
    <AddrLocality>godrej</AddrLocality>
    <AddrLocality_LL></AddrLocality_LL>
    <PinCode>400089</PinCode>
    <DistrictID>530</DistrictID>
    <TalukaID>4284</TalukaID>
    <VillageID>567154</VillageID>
    <FatherFullName>Ashok Rao</FatherFullName>
    <FatherFullName_mr></FatherFullName_mr>
    <Father_Salutation>Shri</Father_Salutation>
    <TrackId>160115001100000001</TrackId>
</ResMessage>
```

---

### 3.2 Step 11: Call pullWebService() web-service from Department

**Testing URL:** http://testcitizenservices.mahaitgov.in/Dept_Authentication.asmx?WSDL

#### Request String Parameters

| Sr No | Parameter | Value | Description |
|-------|-----------|-------|-------------|
| 1 | Track ID | 16011500100000000001 | MahaIT Track ID (Generated in first request) |
| 2 | Client Code | XXXXXX | MahaIT provided client code |
| 3 | User ID | 59fbcf05-f9f7-47d9-ae90-742122c6a292 | User Id provided in First Request |
| 4 | ServiceID | XXXX | 4 digit service id as per given at client registration time |
| 5 | ApplicationID | XXXXXXXXXXXXXXXXXX | Your Unique Order ID/Application ID/Application No. |
| 6 | Payment Status | Y/N | Y = Yes, N = No |
| 7 | Payment Date | YYYY-MM-DD | Provide the date in "YYYY-MM-DD" format only or NA if not available |
| 8 | DigitalSign Status | Y/N | Y = Yes, N = No |
| 9 | DigitalSign Date | YYYY-MM-DD | Provide the date in "YYYY-MM-DD" format only or NA if not available |
| 10 | Estimated ServiceDays | 7 | Only Integer Number or 0 |
| 11 | Estimated Service Date | YYYY-MM-DD | Provide the date in "YYYY-MM-DD" format only or NA if not available |
| 12 | Amount | 100.56 | Amount in decimal format |
| 13 | Request Flag | 0 or 1 or 2 | 0 = to update both status, 1 = to update payment status, 2 = to update digital signature |
| 14 | ApplicationStatus | 1,2,3,4,5 | 1 = Document Pending<br>2 = Payment Pending<br>3 = Under Scrutiny<br>4 = Application Approved<br>5 = Application Rejected |
| 15 | Remark | Alphanumeric | Status of message |
| 16 | UD1 (Compulsory for UD Department) | 123/Int | MahaIT ULB id (Generated in first request) |
| 17 | UD2 (Compulsory for UD Department) | 123/Int | MahaIT ULBDistrict (Generated in first request) |
| 18 | UD3 | NA | User Defined Field 3 |
| 19 | UD4 | NA | User Defined Field 4 |
| 20 | UD5 | NA | User Defined Field 5 |
| 21 | CheckSum | 648349349 | Check Sum Value calculated at department end |

#### 3.2.1 CheckSum Value Generation Process

**Raw string to generate checksum value:**

```
Track ID|Client Code|User ID|ServiceID|ApplicationID|Payment Status|Payment Date|
DigitalSign Status|DigitalSign Date|Estimeted ServiceDays|Estimated Service Date|
Amount|Request Flag|ApplicationStatus|Remark|UD1|UD2|UD3|UD4|UD5|CheckSumKey
```

```csharp
String stringbeforechecksum = Track ID|Client Code|User ID|ServiceID|ApplicationID|
    Payment Status|Payment Date|DigitalSign Status|DigitalSign Date|
    Estimeted ServiceDays|Estimated Service Date|Amount|Request Flag|
    ApplicationStatus|Remark|UD1|UD2|UD3|UD4|UD5|CheckSumKey

String checksumvalue = GenerateCheckSumValue(stringbeforechecksum);
```

**Final string with checksum value:**

```
Track ID|Client Code|User ID|ServiceID|ApplicationID|Payment Status|Payment Date|
DigitalSign Status|DigitalSign Date|Estimeted ServiceDays|Estimated Service Date|
Amount|Request Flag|ApplicationStatus|Remark|UD1|UD2|UD3|UD4|UD5|Checksumvalue
```

#### 3.2.2 Web Method Call (Update Status on Application)

1. Encrypt the final string generated in above point 3.2.1 and then call the web service to update the status of application on Aaple Sarkar Portal.

2. ```csharp
   String EncyKey = SimpleTripleDes(Response, EncryptKey, EncryptIV);
   ```

**Web method Parameters:**

| Parameter | Value | Description |
|-----------|-------|-------------|
| EncyKey | Hdfgkdfgkkk859595mfjkfkkflllf | Encrypted string generated in point no 3.2.2 |
| DeptCode | XXXXXX | MahaIT provided client code |

```csharp
String Response = SetAppStatus(EncyKey, DeptCode);
```

**If updated successfully response will be:**

```xml
<ResMessage><status>Success/Fail</status></ResMessage>
```

**If any error in request parameter then response will be:**

```xml
<ResMessage><error>Error Message</error></ResMessage>
```

---

### 3.3 Step 3: Payment POST Data from Department

This process takes payment from department.

First API to send request is **ValidateRequest API** with below parameters.

#### Request String Parameters

| Sr No | Parameter | Value | Description |
|-------|-----------|-------|-------------|
| 1 | ClientCode | XXXXXX | MahaIT provided client code |
| 2 | CheckSum | 648349349 | Check Sum Value calculated at department end |
| 3 | ServiceID | XXXX | 4 digit service id as per given at client registration time |
| 4 | ApplicationID | XXXXXXXXXXXXXXXXXX | Your Unique Order ID/Application ID/Application No. |
| 5 | Districtid | XXXX | MahaIT provided District code |
| 6 | ApplicationDate | YYYY-MM-DD | Provide the date in "YYYY-MM-DD" format only |
| 7 | TrackID | 16011500100000000001 | MahaIT Track ID (Generated in first request) |
| 8 | User ID | XXXXXXXXXXXXXXXXXX | AS User Identification Number |
| 9 | MobileNo | 9888898888 | Mobile No of User |
| 10 | Name | Xxxxxxx | Full Name of User |
| 11 | Returnurl | XXXXXXXXXXXXXXXXXX | After AS payment where the page land with payment data |
| 12 | UD1 | NA | User Defined Field 1 |
| 13 | UD2 | NA | User Defined Field 2 |
| 14 | UD3 | NA | User Defined Field 3 |
| 15 | UD4 | NA | User Defined Field 4 |
| 16 | UD5 | NA | User Defined Field 5 |

#### Response Key and Errors

If the request is valid then you will receive **Key** and that time Error will be empty, else if the request is not valid then you will receive **Errors** with an empty key.

#### Creating Request for API

Concat all parameters with "|" separated:

**Format:**

```
ClientCode|CheckSum|ServiceID|ApplicationID|Districtid|ApplicationDate|TrackID|
APUserId|MobileNo|Name|returnurl|UD1|UD2|UD3|UD4|UD5
```

```csharp
String finalstring = ClientCode|CheckSum|ServiceID|ApplicationID|Districtid|
    ApplicationDate|TrackID|APUserId|MobileNo|Name|returnurl|UD1|UD2|UD3|UD4|UD5;
```

**Assign to WebString class:**

```csharp
public class WebString
{
    public string webstr { get; set; }
    public string deptcode { get; set;}
}
```

```csharp
String EncyKey = iasObj.SimpleTripleDes(finalstring, EncryptKey, EncryptIV);
WebString objstr = new WebString();
objstr.webstr = EncyKey;
objstr.deptcode = ClientCode;
return objstr;
```

**Set Headers before posting:**

1. Content-Type:application/json
2. Accept:application/json

**Code Snippet For Send Request:**

```csharp
using (var client = new WebClient())
{
    client.Headers.Add("Content-Type:application/json");
    client.Headers.Add("Accept:application/json");
    var serializeddata = JsonConvert.SerializeObject(objstr);
    message = client.UploadString(
        "http://testcitizenservices.MahaITgov.in/en/OutPayment/ValidateRequest",
        serializeddata);
}
```

**After you Get Key, redirect to payment URL:**

```csharp
return Redirect(http://testcitizenservices.MahaITgov.in/en/OutPayment/Pay
    + "?webstr=" + "webstring" + "&DeptCode=" + "Department Code" + "&Authentication" + "Key");
```

#### 3.3.2 GET Data PROCESS

After Payment, department will get response on their provided return URL.

Following parameters are sent in encrypted format querystring:

| Sr No | Parameter | Value | Description |
|-------|-----------|-------|-------------|
| 1 | ClientCode | XXXXXX | MahaIT provided client code |
| 2 | ServiceID | XXXX | 4 digit service id as per given at client registration time |
| 3 | ApplicationID | XXXXXXXXXXXXXXXXXX | Your Unique Order ID/Application ID/Application No. |
| 4 | PaymentTransactionid | 16011500100000000001 | Payment Transaction ID |
| 5 | BankRefID | XXXX | Bank Reference ID |
| 6 | BankRefNo | XXXX | Bank Reference Number |
| 7 | BankID | XXXX | Bank Id |
| 8 | PaymentDate | XXXXXXXXXXXXXXXXXX | Online Payment date |
| 9 | PaymentStatus | True/False | Payment Status |
| 10 | TotalAmount | XXXX | Total Paid amount |

**Finalstring:**

```
Home|3220|1851890033220018F03F|180601125100471865|100001196046|1000011960461|
Atom Bank|01/06/2018 00:00:00|True|23.60
```

```csharp
String EncyKey = iasObj.SimpleTripleDes(finalstring, EncryptKey, EncryptIV);
Post_Url = "Department provided return URL" + "?str=" + EncyKey;
```

---

## Important Notes

### Security Considerations

1. **All communication must be encrypted** using TripleDES with provided keys
2. **Checksum validation** is mandatory for all requests
3. **Token-based authentication** with session management
4. **HTTPS only** for production environments

### Date Format Standards

- **Application Date:** YYYY-MM-DD
- **Display Format:** DD-MMM-YYYY,HH:mm:ss (24-hour, no timezone)

### Status Codes

**Application Status:**
- 1 = Document Pending
- 2 = Payment Pending
- 3 = Under Scrutiny
- 4 = Application Approved
- 5 = Application Rejected

**Payment Status:**
- Y = Yes (Paid)
- N = No (Not Paid)

**Digital Signature Status:**
- Y = Yes (Signed)
- N = No (Not Signed)

### Testing Environment

- **Testing URL:** http://testcitizenservices.MahaITgov.in/Dept_Authentication.asmx?WSDL
- **Production URL:** To be provided by MahaIT

---

## Support

For technical support and integration queries, contact:

**Maharashtra Information Technology Corporation (MahaIT)**
- Email: (Contact MahaIT for support email)
- Website: https://aaplesarkar.maharashtra.gov.in

---

**Document Version:** 3.3
**Last Updated:** 01 June 2018

---

**~~~~~  End of Document ~~~~~**
