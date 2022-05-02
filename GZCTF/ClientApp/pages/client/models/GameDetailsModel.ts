/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type GameDetailsModel = {
    id?: number;
    /**
     * 比赛标题
     */
    title?: string;
    /**
     * 比赛描述
     */
    summary?: string;
    /**
     * 比赛详细介绍
     */
    content?: string;
    /**
     * 队员数量限制
     */
    limit?: number;
    /**
     * 开始时间
     */
    start?: string;
    /**
     * 结束时间
     */
    end?: string;
};
