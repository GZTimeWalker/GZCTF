import {
  Avatar,
  Badge,
  Center,
  Group,
  LoadingOverlay,
  Modal,
  Progress,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core'
import dayjs from 'dayjs'
import { FC, useMemo } from 'react'
import React from 'react'
import { useTranslation } from 'react-i18next'
import { ScoreboardItemModalProps } from '@Components/ScoreboardItemModal'
import { ScrollingText } from '@Components/ScrollingText'
import { TeamRadarMap } from '@Components/charts/TeamRadarMap'
import { useLanguage } from '@Utils/I18n'
import { ChallengeInfo } from '@Api'
import modalClasses from '@Styles/ScoreboardItemModal.module.css'
import tableClasses from '@Styles/Table.module.css'

export const MobileScoreboardItemModal: FC<ScoreboardItemModalProps> = React.memo((props) => {
  const { item, scoreboard, divisionMap, ...modalProps } = props
  const { t } = useTranslation()
  const { locale } = useLanguage()

  const challenges = scoreboard?.challenges
  const challengeIdMap = useMemo(() => {
    if (!challenges) return new Map<number, ChallengeInfo>()
    return Object.keys(challenges).reduce((map, key) => {
      challenges[key].forEach((challenge) => {
        map.set(challenge.id!, challenge)
      })
      return map
    }, new Map<number, ChallengeInfo>())
  }, [challenges])

  const solved = (item?.solvedCount ?? 0) / (scoreboard?.challengeCount ?? 1)

  const indicator = useMemo(() => {
    if (!challenges) return []
    return Object.keys(challenges).map((cate) => ({
      name: cate,
      scoreSum: challenges[cate].reduce((sum, chal) => sum + (!chal.solved ? 0 : chal.score!), 0),
      max: 1,
    }))
  }, [challenges])

  const values = useMemo(() => {
    if (!indicator || !item?.solvedChallenges) return []
    return indicator.map((ind) => {
      const solvedChallenges = item.solvedChallenges!.filter(
        (chal) => challengeIdMap.get(chal.id!)?.category === ind.name
      )
      const cateScore = solvedChallenges.reduce((sum, chal) => sum + chal.score!, 0)
      return Math.min(cateScore / ind.scoreSum, 1)
    })
  }, [indicator, item?.solvedChallenges, challengeIdMap])

  const sortedSolvedChallenges = useMemo(() => {
    if (!item?.solvedChallenges) return []
    return item.solvedChallenges.sort((a, b) => dayjs(b.time).diff(dayjs(a.time)))
  }, [item?.solvedChallenges])

  return (
    <Modal
      {...modalProps}
      classNames={{ header: modalClasses.header, title: modalClasses.titleBar }}
      title={
        <Group justify="left" gap="md" wrap="nowrap" w="100%" className={modalClasses.titleGroup}>
          <Avatar alt="avatar" src={item?.avatar} size={50} radius="md" className={modalClasses.avatar}>
            {item?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0} className={modalClasses.infoWrap}>
            <Group gap={4} wrap="nowrap" className={modalClasses.nameRow}>
              <Title order={4} lineClamp={1} className={modalClasses.teamName} title={item?.name ?? 'Team'}>
                {item?.name ?? 'Team'}
              </Title>
              {item?.divisionId && (
                <Badge size="sm" variant="outline" className={modalClasses.divisionBadge}>
                  {divisionMap.get(item.divisionId) ?? 'Unknown'}
                </Badge>
              )}
            </Group>
            <Text
              size="sm"
              lineClamp={1}
              className={modalClasses.bioText}
              title={item?.bio || t('team.placeholder.bio')}
            >
              {item?.bio || t('team.placeholder.bio')}
            </Text>
          </Stack>
        </Group>
      }
    >
      <Stack align="center" gap="xs">
        <Stack w="60%" miw="20rem">
          <Center h="14rem">
            <LoadingOverlay visible={!indicator || !values} />
            {item && indicator && values && (
              <TeamRadarMap indicator={indicator} value={values} name={item?.name ?? ''} />
            )}
          </Center>
          <Group grow ta="center">
            <Stack gap={1}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.rank || '-'}
              </Text>
              <Text size="xs">{t('game.label.score_table.rank_total')}</Text>
            </Stack>
            {item?.divisionId && (
              <Stack gap={1}>
                <Text fw="bold" size="sm" ff="monospace">
                  {item?.divisionRank || '-'}
                </Text>
                <Text size="xs">{t('game.label.score_table.rank_division')}</Text>
              </Stack>
            )}
            <Stack gap={1}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.score}
              </Text>
              <Text size="xs">{t('game.label.score_table.score')}</Text>
            </Stack>
            <Stack gap={1}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.solvedCount}
              </Text>
              <Text size="xs">{t('game.label.score_table.solved_count')}</Text>
            </Stack>
          </Group>
          <Progress value={solved * 100} size="sm" />
        </Stack>
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%">
            <Table className={tableClasses.table} fz="sm">
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('common.label.challenge')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.score')}</Table.Th>
                  <Table.Th>{t('common.label.time')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {sortedSolvedChallenges &&
                  challengeIdMap &&
                  sortedSolvedChallenges.map((chal) => {
                    const info = challengeIdMap.get(chal.id!)
                    return (
                      <Table.Tr key={chal.id} ff="monospace">
                        <Table.Td>
                          <ScrollingText text={info?.title ?? ''} />
                        </Table.Td>
                        <Table.Td>{chal.score}</Table.Td>
                        <Table.Td>{dayjs(chal.time).locale(locale).format('SL HH:mm')}</Table.Td>
                      </Table.Tr>
                    )
                  })}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        ) : (
          <Text py="1rem" fw="bold">
            {t('game.placeholder.no_solved')}
          </Text>
        )}
      </Stack>
    </Modal>
  )
})
