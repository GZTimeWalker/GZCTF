/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { TimeLine } from './TimeLine';

export type TopTimeLine = {
    /**
     * 队伍Id
     */
    id?: number;
    /**
     * 队伍名称
     */
    name?: string;
    /**
     * 时间线
     */
    items?: Array<TimeLine>;
};
