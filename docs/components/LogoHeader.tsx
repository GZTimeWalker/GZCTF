import { FC } from "react";
import MainIcon from "@Components/icon/MainIcon";
import { useTheme } from "next-themes";

export const LogoHeader: FC = () => {
  const { resolvedTheme } = useTheme();
  const isDark = resolvedTheme === "dark";
  const color = isDark ? "#fff" : "#414141";
  const highlightColor = isDark ? "#0AD7AF" : "#02BFA5";

  return (
    <div style={{ display: "flex", alignItems: "center", gap: "2px" }}>
      <MainIcon width={40} height={40}/>
      <span style={{ fontWeight: 800, fontSize: "30px", color }}>
        GZ<span style={{ color: highlightColor }}>::</span>CTF
      </span>
    </div>
  );
};

export default LogoHeader;
