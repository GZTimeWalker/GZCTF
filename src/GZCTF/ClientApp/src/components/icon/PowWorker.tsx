import cx from 'clsx'
import { FC } from 'react'
import classes from '@Styles/PowWorker.module.css'

interface PowWorkerProps {
  done?: boolean
}

export const PowWorker: FC<PowWorkerProps> = ({ done }) => {
  return (
    <svg
      id="pow"
      xmlns="http://www.w3.org/2000/svg"
      width="144"
      height="24"
      viewBox="0 0 144 24"
      className={classes.box}
      data-done={done}
    >
      <g fill="none" strokeWidth="1.6">
        <path
          className={classes.left}
          data-done={done}
          strokeWidth="4"
          strokeLinecap="square"
          strokeLinejoin="bevel"
          d="M 16.943392,20.175366 2.7831628,11.99999 16.943392,3.8246236 Z"
        />
        <path data-done={done} className={cx(classes.l, classes.l1)} d="M 30,6 H 117" />
        <path data-done={done} className={cx(classes.l, classes.l2)} d="M 38,12 H 140" />
        <path data-done={done} className={cx(classes.l, classes.l3)} d="M 36,18 H 120" />
      </g>
    </svg>
  )
}
