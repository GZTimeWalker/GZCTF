import React, { FC } from "react";
import { useTheme } from "next-themes";
import type { DocsThemeConfig } from "nextra-theme-docs";

const logo: FC = () => {
  const { resolvedTheme } = useTheme();
  const isDark = resolvedTheme === "dark";
  const color = isDark ? "#fff" : "#414141";
  const highlightColor = isDark ? "#0AD7AF" : "#02BFA5";

  return (
    <div style={{ display: "flex", alignItems: "center", gap: "2px" }}>
      <span>
        <svg width="40" viewBox="0 0 4800 4800">
          <path
            id="Triangle"
            fill={color}
            fillRule="evenodd"
            d="M2994.48,4244.61L505.28,2807.47V1992.53l256.572-148.14L1287,2285l258-307,160.39,135.56L1209.27,2400l1786.1,1031.21V2427.79L3517,1806,2420.98,886.5l573.5-331.11,705.76,407.474V3837.14Z"
          />
          <g id="Flag">
            <path
              id="Flag_0"
              fill="#00bfa5"
              fillRule="evenodd"
              d="M1280.55,582.029L2046.6,1224.82l-771.35,919.25L509.21,1501.28Z"
            />
            <path
              id="Flag_1"
              fill="#007f6e"
              fillRule="evenodd"
              d="M1225.95,1580.54l306.42,257.11-257.12,306.42Z"
            />
            <path
              id="Flag_2"
              fill="#1de9b6"
              fillRule="evenodd"
              d="M2636.97,2699.25l-32.14,38.31-264.98,315.78L1812.4,2748.51l332.8-396.63-919.25-771.34L1997.3,661.284,3376.18,1818.3ZM1880,3601.24l0.15-.04L1351.4,4231.34,891.769,3845.67l460.361-548.64Z"
            />
          </g>
        </svg>
      </span>
      <span style={{ fontWeight: 800, fontSize: "30px", color }}>
        GZ<span style={{ color: highlightColor }}>::</span>CTF
      </span>
    </div>
  );
};

const config: DocsThemeConfig = {
  docsRepositoryBase: "https://github.com/GZTimeWalker/GZCTF/tree/main/docs",
  useNextSeoProps() {
    return {
      titleTemplate: "%s - GZ::CTF",
    };
  },
  head: (
    <>
      <link rel="icon" type="image/png" href="/favicon.png" />
    </>
  ),
  logo,
  project: {
    link: "https://github.com/GZTimeWalker/GZCTF",
  },
  search: {
    component: null,
  },
  i18n: [
    // { locale: "en", text: "English" },
    { locale: "zh", text: "中文" },
  ],
  feedback: {
    content: "文档有问题？欢迎反馈 →",
  },
  editLink: {
    text: "编辑此页面",
  },
  footer: {
    text: (
      <div style={{ width: "100%", textAlign: "center" }}>
        ©2022-present By GZTimeWalker
      </div>
    ),
  },
};

export default config;
