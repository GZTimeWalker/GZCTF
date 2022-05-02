/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { Challenge } from './Challenge';
import type { FileType } from './FileType';
import type { LocalFile } from './LocalFile';

export type FlagContext = {
    id?: number;
    /**
     * Flag 内容
     */
    flag: string;
    /**
     * 附件类型
     */
    attachmentType: FileType;
    /**
     * Flag 对应附件 (远程文件）
     */
    remoteUrl?: string | null;
    /**
     * Flag 对应文件（本地文件）
     */
    localFile?: LocalFile | null;
    /**
     * 是否已被占用
     */
    isOccupied?: boolean;
    /**
     * 赛题
     */
    challenge?: Challenge | null;
    /**
     * 赛题Id
     */
    challengeId?: number;
    /**
     * 附件访问链接
     */
    url?: string | null;
};
