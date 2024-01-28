const withNextra = require("nextra")({
  latex: true,
  theme: "nextra-theme-docs",
  themeConfig: "./theme.config.tsx",
});

module.exports = withNextra({
  reactStrictMode: true,
  i18n: {
    locales: ["zh", "ja"],
    defaultLocale: "zh",
    localeDetection: false,
  },
});
