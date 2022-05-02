/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeInfo } from './ChallengeInfo';
import type { ScoreboardItem } from './ScoreboardItem';
import type { TopTimeLine } from './TopTimeLine';

export type ScoreboardModel = {
    /**
     * 更新时间
     */
    updateTimeUTC?: string;
    /**
     * 前十名的时间线
     */
    timeLine?: Array<TopTimeLine>;
    /**
     * 队伍信息
     */
    items?: Array<ScoreboardItem>;
    /**
     * 题目信息
     */
    challenges?: Record<string, Array<ChallengeInfo>>;
};
