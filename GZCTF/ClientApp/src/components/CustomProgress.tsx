import { FC } from "react";
import "./CustomProgress.css";

export interface CustomProgressProps {
  thickness?: number,
  percentage?: number,
  spikeLengthPrct?: number,
  paddingY?: number,
  color?: string,
}

const CustomProgress: FC<CustomProgressProps> = (props: CustomProgressProps) => {
  const spikeLength = props.spikeLengthPrct || 250;
  const thickness = props.thickness || 4;
  const paddingY = props.paddingY === 0 ? 0 : props.paddingY || thickness * spikeLength / 100;
  const spikeColor = (props.color || "#ffffff") + "88";

  return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: paddingY + "px 0" }}>
      <div style={{ display: "flex", alignItems: "center", height: thickness, width: "100%", backgroundColor: "#80808022" }}>
        <div style={{
          position: "relative",
          display: "flex",
          flexDirection: "row",
          justifyContent: "end",
          height: "100%",
          width: (props.percentage === 0 ? 0 : props.percentage || 75) + "%",
          minWidth: thickness,
          backgroundColor: (props.color || "#ffffff") + "66"
        }}>
          <div className="progress-pulse-container">
            <div style={{ background: `linear-gradient(-90deg, ${spikeColor}, #fff0)` }}></div>
          </div>
          <div className="spikes-group" style={{ position: "relative", height: "100%", aspectRatio: "1 / 1", backgroundColor: props.color || "#fff" }}>
            <div style={{ position: "absolute", left: 0, top: "-" + spikeLength + "%", height: spikeLength + "%", width: "100%", background: `linear-gradient(0deg, ${spikeColor}, #fff0)` }}></div>
            <div style={{ position: "absolute", left: 0, bottom: "-" + spikeLength + "%", height: spikeLength + "%", width: "100%", background: `linear-gradient(180deg, ${spikeColor}, #fff0)` }}></div>
            <div style={{ position: "absolute", left: "-" + spikeLength + "%", top: 0, height: "100%", width: spikeLength + "%", background: `linear-gradient(-90deg, ${spikeColor}, #fff0)` }}></div>
            <div style={{ position: "absolute", right: "-" + spikeLength + "%", top: 0, height: "100%", width: spikeLength + "%", background: `linear-gradient(90deg, ${spikeColor}, #fff0)` }}></div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default CustomProgress;
