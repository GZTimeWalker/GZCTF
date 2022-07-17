/** @type {import('next').NextConfig} */

const TARGET = 'http://localhost:5000';

const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return {
      fallback: [
        {
          source: '/api/:path*',
          destination: `${TARGET}/api/:path*`,
        },
        {
          source: '/swagger/:path*',
          destination: `${TARGET}/swagger/:path*`,
        },
        {
          source: '/assets/:path*',
          destination: `${TARGET}/assets/:path*`,
        },
        {
          source: '/hub/:path*',
          destination: `${TARGET}/hub/:path*`,
        },
      ],
    };
  },
};

module.exports = nextConfig;
