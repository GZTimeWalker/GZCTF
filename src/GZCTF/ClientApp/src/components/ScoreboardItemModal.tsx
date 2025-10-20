import {
  Avatar,
  Badge,
  Center,
  Group,
  Modal,
  ModalProps,
  Progress,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
} from '@mantine/core'
import dayjs from 'dayjs'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { MemberContributionPie } from '@Components/charts/MemberContributionPie'
import { MemberContributionPieProps } from '@Components/charts/MemberContributionPie'
import { TeamRadarMap, TeamRadarMapProps } from '@Components/charts/TeamRadarMap'
import { useLanguage } from '@Utils/I18n'
import { BloodsTypes, BonusLabel } from '@Utils/Shared'
import { ChallengeInfo, ScoreboardItem, ScoreboardModel, SubmissionType } from '@Api'
import modalClasses from '@Styles/ScoreboardItemModal.module.css'
import tableClasses from '@Styles/Table.module.css'
import { ScrollingText } from './ScrollingText'

export interface ScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  divisionMap: Map<number, string>
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  scoreboard?: ScoreboardModel
}

function calculateScoreRadar(
  challenges: Record<string, ChallengeInfo[]>,
  challengeIdMap: Map<number, ChallengeInfo>,
  item?: ScoreboardItem
): TeamRadarMapProps {
  const indicator =
    challenges &&
    Object.keys(challenges).map((cate) => ({
      name: cate,
      scoreSum: challenges[cate].reduce((sum, chal) => sum + (!chal.solved ? 0 : chal.score!), 0),
      max: 1,
    }))

  const value = indicator?.map((ind) => {
    const solvedChallenges = item?.solvedChallenges?.filter(
      (chal) => challengeIdMap?.get(chal.id!)?.category === ind.name
    )
    const cateScore = solvedChallenges?.reduce((sum, chal) => sum + chal.score!, 0) ?? 0
    return Math.min(cateScore / ind.scoreSum, 1)
  })

  return { indicator, value, name: item?.name ?? '' }
}

function calculateMemberContribution(item?: ScoreboardItem): MemberContributionPieProps {
  const memberScores =
    item?.solvedChallenges?.reduce((acc, chal) => {
      const score = acc.get(chal.userName!) ?? 0
      acc.set(chal.userName!, score + chal.score!)
      return acc
    }, new Map<string, number>()) ?? new Map<string, number>()

  const data = Array.from(memberScores.entries()).map(([name, value]) => ({
    name,
    value,
  }))

  data.sort((a, b) => b.value - a.value)

  return { data }
}

export const ScoreboardItemModal: FC<ScoreboardItemModalProps> = (props) => {
  const { item, scoreboard, bloodBonusMap, divisionMap, ...modalProps } = props
  const { t } = useTranslation()
  const { locale } = useLanguage()
  const challenges = scoreboard?.challenges
  const challengeIdMap =
    challenges &&
    Object.keys(challenges).reduce((map, key) => {
      challenges[key].forEach((challenge) => {
        map.set(challenge.id!, challenge)
      })
      return map
    }, new Map<number, ChallengeInfo>())

  const valid = item && challenges && challengeIdMap
  const solved = (item?.solvedCount ?? 0) / (scoreboard?.challengeCount ?? 1)

  const radarData = useMemo(() => {
    if (!valid) return null
    return calculateScoreRadar(challenges, challengeIdMap, item)
  }, [valid, challenges, challengeIdMap, item])

  const memberContributionData = useMemo(() => {
    if (!valid) return null
    return calculateMemberContribution(item)
  }, [valid, item])

  return (
    <Modal
      {...modalProps}
      classNames={{ header: modalClasses.header, title: modalClasses.titleBar }}
      title={
        <Group justify="left" gap="md" wrap="nowrap" className={modalClasses.titleGroup}>
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
        <Stack w="85%" miw="20rem">
          <Center h="14rem">
            {valid && radarData && memberContributionData && (
              <Group wrap="nowrap" gap={0} justify="center" w="100%" h="100%">
                <TeamRadarMap {...radarData} />
                <MemberContributionPie {...memberContributionData} />
              </Group>
            )}
          </Center>
          <Group grow ta="center">
            <Stack gap={2}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.rank || '-'}
              </Text>
              <Text size="xs" fw={500}>
                {t('game.label.score_table.rank_total')}
              </Text>
            </Stack>
            {item?.divisionId && (
              <Stack gap={2}>
                <Text fw="bold" size="sm" ff="monospace">
                  {item?.divisionRank || '-'}
                </Text>
                <Text size="xs" fw={500}>
                  {t('game.label.score_table.rank_division')}
                </Text>
              </Stack>
            )}
            <Stack gap={2}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.score}
              </Text>
              <Text size="xs" fw={500}>
                {t('game.label.score_table.score')}
              </Text>
            </Stack>
            <Stack gap={2}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.solvedCount}
              </Text>
              <Text size="xs" fw={500}>
                {t('game.label.score_table.solved_count')}
              </Text>
            </Stack>
          </Group>
          <Progress value={solved * 100} />
        </Stack>
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%" scrollbars="y">
            <Table className={tableClasses.table}>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('common.label.user')}</Table.Th>
                  <Table.Th>{t('common.label.challenge')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.type')}</Table.Th>
                  <Table.Th>{t('game.label.score_table.score')}</Table.Th>
                  <Table.Th>{t('common.label.time')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {item?.solvedChallenges &&
                  challengeIdMap &&
                  item.solvedChallenges
                    .sort((a, b) => dayjs(b.time).diff(dayjs(a.time)))
                    .map((chal) => {
                      const info = challengeIdMap.get(chal.id!)!
                      return (
                        <Table.Tr key={chal.id}>
                          <Table.Td fw="bold">{chal.userName}</Table.Td>
                          <Table.Td>
                            <ScrollingText text={info.title} miw="14rem" maw="20rem" />
                          </Table.Td>
                          <Table.Td fz="sm">{info.category}</Table.Td>
                          <Table.Td ff="monospace" fz="sm">
                            {chal.score}
                            {info.score && chal.score! > info.score && chal.type && BloodsTypes.includes(chal.type) && (
                              <Text size="sm" c="dimmed" span>
                                {`(${bloodBonusMap.get(chal.type)?.descr})`}
                              </Text>
                            )}
                          </Table.Td>
                          <Table.Td ff="monospace" fz="sm">
                            {dayjs(chal.time).locale(locale).format('SL HH:mm:ss')}
                          </Table.Td>
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
}
