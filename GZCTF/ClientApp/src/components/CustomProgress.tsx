import { FC, useState } from "react";
import "./CustomProgress.css";

export interface CustomProgressProps {
  thickness?: number,
  percentage?: number,
  spikeLengthPrct?: number,
  paddingY?: number,
}

const CustomProgress: FC<CustomProgressProps> = (props: CustomProgressProps) => {
  const [spikeLength, setSpikeLength] = useState(props.spikeLengthPrct ? props.spikeLengthPrct : 250);
  const [thickness, setThickness] = useState(props.thickness ? props.thickness : 4);
  const [paddingY, setPaddingY] = useState(props.paddingY == 0 ? 0 : props.paddingY ? props.paddingY : thickness * spikeLength / 100);

  return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: paddingY + "px 0" }}>
      <div style={{ display: "flex", alignItems: "center", height: thickness, width: "100%", backgroundColor: "#fff2" }}>
        <div style={{ position: "relative", display: "flex", flexDirection: "row", justifyContent: "end", height: "100%", width: (props.percentage === 0 ? 0 : props.percentage ? props.percentage : 75) + "%", minWidth: thickness, backgroundColor: "#fff6" }}>
          <div className="progress-pulse-container">
            <div></div>
          </div>
          <div className="spikes-group" style={{ position: "relative", height: "100%", aspectRatio: "1 / 1", backgroundColor: "#fff" }}>
            <div style={{ position: "absolute", left: 0, top: "-" + spikeLength + "%", height: spikeLength + "%", width: "100%", background: "linear-gradient(0deg, #fff8, #fff0)" }}></div>
            <div style={{ position: "absolute", left: 0, bottom: "-" + spikeLength + "%", height: spikeLength + "%", width: "100%", background: "linear-gradient(180deg, #fff8, #fff0)" }}></div>
            <div style={{ position: "absolute", left: "-" + spikeLength + "%", top: 0, height: "100%", width: spikeLength + "%", background: "linear-gradient(-90deg, #fff8, #fff0)" }}></div>
            <div style={{ position: "absolute", right: "-" + spikeLength + "%", top: 0, height: "100%", width: spikeLength + "%", background: "linear-gradient(90deg, #fff8, #fff0)" }}></div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default CustomProgress