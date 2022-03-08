import React, { SVGProps } from 'react';

export function RoadMapIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg width="1em" height="1em" viewBox="0 0 32 32" {...props}>
      <path
        d="M12 30H4a2.002 2.002 0 0 1-2-2v-4a2.002 2.002 0 0 1 2-2h8a2.002 2.002 0 0 1 2 2v4a2.002 2.002 0 0 1-2 2zm-8-6v4h8v-4z"
        fill="currentColor"
      ></path>
      <path
        d="M28 20H12a2.002 2.002 0 0 1-2-2v-4a2.002 2.002 0 0 1 2-2h16a2.002 2.002 0 0 1 2 2v4a2.002 2.002 0 0 1-2 2zm-16-6v4h16v-4z"
        fill="currentColor"
      ></path>
      <path
        d="M16 10H4a2.002 2.002 0 0 1-2-2V4a2.002 2.002 0 0 1 2-2h12a2.002 2.002 0 0 1 2 2v4a2.002 2.002 0 0 1-2 2zM4 4v4h12V4z"
        fill="currentColor"
      ></path>
    </svg>
  );
}
