/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { EventType } from './EventType';

/**
 * 比赛事件，记录但不会发往客户端。
 * 信息涵盖Flag提交信息、容器启动关闭信息、作弊信息、题目分数变更信息
 */
export type GameEvent = {
    /**
     * 事件类型
     */
    type: EventType;
    /**
     * 事件内容
     */
    content: string;
    /**
     * 发布时间
     */
    time: string;
    /**
     * 相关用户名
     */
    user?: string;
    /**
     * 相关队伍名
     */
    team?: string;
};
