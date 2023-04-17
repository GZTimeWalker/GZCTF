const withNextra = require("nextra")({
  theme: "nextra-theme-docs",
  themeConfig: "./theme.config.jsx",
});

module.exports = withNextra({
  i18n: {
    locales: ["zh"],
    defaultLocale: "zh",
    localeDetection: false,
  },
});
