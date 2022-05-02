/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { NoticeType } from './NoticeType';

/**
 * 比赛通知，会发往客户端。
 * 信息涵盖一二三血通知、提示发布通知、题目开启通知等
 */
export type GameNotice = {
    /**
     * 通知类型
     */
    type: NoticeType;
    /**
     * 通知内容
     */
    content: string;
    /**
     * 发布时间
     */
    time: string;
};
