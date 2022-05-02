/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type FlagInfoModel = {
    /**
     * Flag文本
     */
    flag?: string;
    /**
     * Flag 对应附件（本地文件哈希）
     */
    fileHash?: string | null;
    /**
     * Flag 对应附件 (远程文件）
     */
    url?: string | null;
};
