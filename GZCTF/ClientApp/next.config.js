/** @type {import('next').NextConfig} */

const TARGET = 'http://localhost:5000';

const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${TARGET}/api/:path*`
      },
      {
        source: '/swagger/:path*',
        destination: `${TARGET}/swagger/:path*`
      },
      {
        source: '/hub/:path*',
        destination: `${TARGET}/hub/:path*`
      }
    ]
  }
}

module.exports = nextConfig
