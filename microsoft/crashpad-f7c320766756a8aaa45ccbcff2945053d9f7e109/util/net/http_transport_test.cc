// Copyright 2014 The Crashpad Authors. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#include "util/net/http_transport.h"

#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <sys/types.h>

#include <memory>
#include <utility>
#include <vector>

#include "base/files/file_path.h"
#include "base/format_macros.h"
#include "base/logging.h"
#include "base/strings/stringprintf.h"
#include "base/strings/utf_string_conversions.h"
#include "build/build_config.h"
#include "gtest/gtest.h"
#include "test/multiprocess_exec.h"
#include "test/test_paths.h"
#include "util/file/file_io.h"
#include "util/misc/random_string.h"
#include "util/net/http_body.h"
#include "util/net/http_headers.h"
#include "util/net/http_multipart_builder.h"

namespace crashpad {
namespace test {
namespace {

class HTTPTransportTestFixture : public MultiprocessExec {
 public:
  using RequestValidator =
      void(*)(HTTPTransportTestFixture*, const std::string&);

  HTTPTransportTestFixture(const HTTPHeaders& headers,
                           std::unique_ptr<HTTPBodyStream> body_stream,
                           uint16_t http_response_code,
                           RequestValidator request_validator)
      : MultiprocessExec(),
        headers_(headers),
        body_stream_(std::move(body_stream)),
        response_code_(http_response_code),
        request_validator_(request_validator) {
    base::FilePath server_path = TestPaths::TestDataRoot().Append(
        FILE_PATH_LITERAL("util/net/http_transport_test_server.py"));
#if defined(OS_POSIX)
    SetChildCommand(server_path.value(), nullptr);
#elif defined(OS_WIN)
    // Explicitly invoke a shell and python so that python can be found in the
    // path, and run the test script.
    std::vector<std::string> args;
    args.push_back("/c");
    args.push_back("python");
    args.push_back(base::UTF16ToUTF8(server_path.value()));
    SetChildCommand(getenv("COMSPEC"), &args);
#endif  // OS_POSIX
  }

  const HTTPHeaders& headers() { return headers_; }

 private:
  void MultiprocessParent() override {
    // Use Logging*File() instead of Checked*File() so that the test can fail
    // gracefully with a gtest assertion if the child does not execute properly.

    // The child will write the HTTP server port number as a packed unsigned
    // short to stdout.
    uint16_t port;
    ASSERT_TRUE(LoggingReadFileExactly(ReadPipeHandle(), &port, sizeof(port)));

    // Then the parent will tell the web server what response code to send
    // for the HTTP request.
    ASSERT_TRUE(LoggingWriteFile(
        WritePipeHandle(), &response_code_, sizeof(response_code_)));

    // The parent will also tell the web server what response body to send back.
    // The web server will only send the response body if the response code is
    // 200.
    const std::string random_string = RandomString();

    ASSERT_TRUE(LoggingWriteFile(WritePipeHandle(),
                                 random_string.c_str(),
                                 random_string.size()));

    // Now execute the HTTP request.
    std::unique_ptr<HTTPTransport> transport(HTTPTransport::Create());
    transport->SetMethod("POST");
    transport->SetURL(base::StringPrintf("http://127.0.0.1:%d/upload", port));
    for (const auto& pair : headers_) {
      transport->SetHeader(pair.first, pair.second);
    }
    transport->SetBodyStream(std::move(body_stream_));

    std::string response_body;
    bool success = transport->ExecuteSynchronously(&response_body);
    if (response_code_ == 200) {
      EXPECT_TRUE(success);
      std::string expect_response_body = random_string + "\r\n";
      EXPECT_EQ(expect_response_body, response_body);
    } else {
      EXPECT_FALSE(success);
      EXPECT_TRUE(response_body.empty());
    }

    // Read until the child's stdout closes.
    std::string request;
    char buf[32];
    FileOperationResult bytes_read;
    while ((bytes_read = ReadFile(ReadPipeHandle(), buf, sizeof(buf))) != 0) {
      ASSERT_GE(bytes_read, 0);
      request.append(buf, bytes_read);
    }

    if (request_validator_)
      request_validator_(this, request);
  }

  HTTPHeaders headers_;
  std::unique_ptr<HTTPBodyStream> body_stream_;
  uint16_t response_code_;
  RequestValidator request_validator_;
};

const char kMultipartFormData[] = "multipart/form-data";

void GetHeaderField(const std::string& request,
                    const std::string& header,
                    std::string* value) {
  size_t index = request.find(header);
  ASSERT_NE(std::string::npos, index);
  // Since the header is never the first line of the request, it should always
  // be preceded by a CRLF.
  EXPECT_EQ('\n', request[index - 1]);
  EXPECT_EQ('\r', request[index - 2]);

  index += header.length();
  EXPECT_EQ(':', request[index++]);
  // Per RFC 7230 §3.2, there can be one or more spaces or horizontal tabs.
  // For testing purposes, just assume one space.
  EXPECT_EQ(' ', request[index++]);

  size_t header_end = request.find('\r', index);
  ASSERT_NE(std::string::npos, header_end);

  *value = request.substr(index, header_end - index);
}

void GetMultipartBoundary(const std::string& request,
                          std::string* multipart_boundary) {
  std::string content_type;
  GetHeaderField(request, kContentType, &content_type);

  ASSERT_GE(content_type.length(), strlen(kMultipartFormData));
  size_t index = strlen(kMultipartFormData);
  EXPECT_EQ(kMultipartFormData, content_type.substr(0, index));

  EXPECT_EQ(';', content_type[index++]);

  size_t boundary_begin = content_type.find('=', index);
  ASSERT_NE(std::string::npos, boundary_begin);
  EXPECT_EQ('=', content_type[boundary_begin++]);
  if (multipart_boundary) {
    *multipart_boundary = content_type.substr(boundary_begin);
  }
}

const char kBoundaryEq[] = "boundary=";

void ValidFormData(HTTPTransportTestFixture* fixture,
                   const std::string& request) {
  std::string actual_boundary;
  GetMultipartBoundary(request, &actual_boundary);

  const auto& content_type = fixture->headers().find(kContentType);
  ASSERT_NE(fixture->headers().end(), content_type);

  size_t boundary = content_type->second.find(kBoundaryEq);
  ASSERT_NE(std::string::npos, boundary);
  std::string expected_boundary =
      content_type->second.substr(boundary + strlen(kBoundaryEq));
  EXPECT_EQ(expected_boundary, actual_boundary);

  size_t body_start = request.find("\r\n\r\n");
  ASSERT_NE(std::string::npos, body_start);
  body_start += 4;

  std::string expected = "--" + expected_boundary + "\r\n";
  expected += "Content-Disposition: form-data; name=\"key1\"\r\n\r\n";
  expected += "test\r\n";
  ASSERT_LT(body_start + expected.length(), request.length());
  EXPECT_EQ(expected, request.substr(body_start, expected.length()));

  body_start += expected.length();

  expected = "--" + expected_boundary + "\r\n";
  expected += "Content-Disposition: form-data; name=\"key2\"\r\n\r\n";
  expected += "--abcdefg123\r\n";
  expected += "--" + expected_boundary + "--\r\n";
  ASSERT_EQ(body_start + expected.length(), request.length());
  EXPECT_EQ(expected, request.substr(body_start));
}

TEST(HTTPTransport, ValidFormData) {
  HTTPMultipartBuilder builder;
  builder.SetFormData("key1", "test");
  builder.SetFormData("key2", "--abcdefg123");

  HTTPHeaders headers;
  builder.PopulateContentHeaders(&headers);

  HTTPTransportTestFixture test(
      headers, builder.GetBodyStream(), 200, &ValidFormData);
  test.Run();
}

TEST(HTTPTransport, ValidFormData_Gzip) {
  HTTPMultipartBuilder builder;
  builder.SetGzipEnabled(true);
  builder.SetFormData("key1", "test");
  builder.SetFormData("key2", "--abcdefg123");

  HTTPHeaders headers;
  builder.PopulateContentHeaders(&headers);

  HTTPTransportTestFixture test(headers, builder.GetBodyStream(), 200,
      &ValidFormData);
  test.Run();
}

const char kTextPlain[] = "text/plain";

void ErrorResponse(HTTPTransportTestFixture* fixture,
                   const std::string& request) {
  std::string content_type;
  GetHeaderField(request, kContentType, &content_type);
  EXPECT_EQ(kTextPlain, content_type);
}

TEST(HTTPTransport, ErrorResponse) {
  HTTPMultipartBuilder builder;
  HTTPHeaders headers;
  headers[kContentType] = kTextPlain;
  HTTPTransportTestFixture test(headers, builder.GetBodyStream(),
      404, &ErrorResponse);
  test.Run();
}

const char kTextBody[] = "hello world";

void UnchunkedPlainText(HTTPTransportTestFixture* fixture,
                        const std::string& request) {
  std::string header_value;
  GetHeaderField(request, kContentType, &header_value);
  EXPECT_EQ(kTextPlain, header_value);

  GetHeaderField(request, kContentLength, &header_value);
  const auto& content_length = fixture->headers().find(kContentLength);
  ASSERT_NE(fixture->headers().end(), content_length);
  EXPECT_EQ(content_length->second, header_value);

  size_t body_start = request.rfind("\r\n");
  ASSERT_NE(std::string::npos, body_start);

  EXPECT_EQ(kTextBody, request.substr(body_start + 2));
}

TEST(HTTPTransport, UnchunkedPlainText) {
  std::unique_ptr<HTTPBodyStream> body_stream(
      new StringHTTPBodyStream(kTextBody));

  HTTPHeaders headers;
  headers[kContentType] = kTextPlain;
  headers[kContentLength] = base::StringPrintf("%" PRIuS, strlen(kTextBody));

  HTTPTransportTestFixture test(
      headers, std::move(body_stream), 200, &UnchunkedPlainText);
  test.Run();
}

void RunUpload33k(bool has_content_length) {
  // On macOS, NSMutableURLRequest winds up calling into a CFReadStream’s Read()
  // callback with a 32kB buffer. Make sure that it’s able to get everything
  // when enough is available to fill this buffer, requiring more than one
  // Read().

  std::string request_string(33 * 1024, 'a');
  std::unique_ptr<HTTPBodyStream> body_stream(
      new StringHTTPBodyStream(request_string));

  HTTPHeaders headers;
  headers[kContentType] = "application/octet-stream";
  if (has_content_length) {
    headers[kContentLength] =
        base::StringPrintf("%" PRIuS, request_string.size());
  }
  HTTPTransportTestFixture test(
      headers,
      std::move(body_stream),
      200,
      [](HTTPTransportTestFixture* fixture, const std::string& request) {
        size_t body_start = request.rfind("\r\n");
        EXPECT_EQ(33 * 1024u + 2, request.size() - body_start);
      });
  test.Run();
}

TEST(HTTPTransport, Upload33k) {
  RunUpload33k(true);
}

TEST(HTTPTransport, Upload33k_LengthUnknown) {
  // The same as Upload33k, but without declaring Content-Length ahead of time.
  RunUpload33k(false);
}

}  // namespace
}  // namespace test
}  // namespace crashpad
