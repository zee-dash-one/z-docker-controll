function FindProxyForURL(url, host) {
  host = host.toLowerCase();
  if (dnsDomainIs(host, ".mandrill-court.ts.net") || host === "mandrill-court.ts.net") {
    return "PROXY 127.0.0.1:1056";
  }
  return "DIRECT";
}
