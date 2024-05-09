import {
  Avatar,
  Badge,
  Center,
  Group,
  LoadingOverlay,
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
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import TeamRadarMap from '@Components/TeamRadarMap'
import { BloodsTypes, BonusLabel } from '@Utils/Shared'
import { useTableStyles } from '@Utils/ThemeOverride'
import { ChallengeInfo, ScoreboardItem, SubmissionType } from '@Api'

interface ScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  challenges?: Record<string, ChallengeInfo[]>
}

const ScoreboardItemModal: FC<ScoreboardItemModalProps> = (props) => {
  const { item, challenges, bloodBonusMap, ...modalProps } = props
  const { classes, theme } = useTableStyles()

  const { t } = useTranslation()

  const challengeIdMap =
    challenges &&
    Object.keys(challenges).reduce((map, key) => {
      challenges[key].forEach((challenge) => {
        map.set(challenge.id!, challenge)
      })
      return map
    }, new Map<number, ChallengeInfo>())

  const solved = (item?.solvedCount ?? 0) / (item?.challenges?.length ?? 1)

  const indicator =
    challenges &&
    Object.keys(challenges).map((tag) => ({
      name: tag,
      scoreSum: challenges[tag].reduce((sum, chal) => sum + (!chal.solved ? 0 : chal.score!), 0),
      max: 1,
    }))

  const values = new Array(item?.challenges?.length ?? 0).fill(0)

  item?.challenges?.forEach((chal) => {
    if (indicator && challengeIdMap && chal) {
      const challenge = challengeIdMap.get(chal.id!)
      const index = challenge && indicator?.findIndex((ch) => ch.name === challenge.tag)
      if (chal?.score && challenge?.score && index !== undefined && index !== -1) {
        values[index] += challenge?.score / indicator[index].scoreSum
      }
    }
  })

  return (
    <Modal
      {...modalProps}
      title={
        <Group justify="left" gap="md" wrap="nowrap">
          <Avatar alt="avatar" src={item?.avatar} size={50} radius="md" color={theme.primaryColor}>
            {item?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0}>
            <Group gap={4}>
              <Title order={4} lineClamp={1} fw="bold">
                {item?.name ?? 'Team'}
              </Title>
              {item?.organization && (
                <Badge size="sm" variant="outline">
                  {item.organization}
                </Badge>
              )}
            </Group>
            <Text truncate size="sm" lineClamp={1}>
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
            <Stack gap={2}>
              <Text fw="bold" size="sm" className={classes.mono}>
                {item?.rank}
              </Text>
              <Text size="xs">{t('game.label.score_table.rank_total')}</Text>
            </Stack>
            {item?.organization && (
              <Stack gap={2}>
                <Text fw="bold" size="sm" className={classes.mono}>
                  {item?.organizationRank}
                </Text>
                <Text size="xs">{t('game.label.score_table.rank_organization')}</Text>
              </Stack>
            )}
            <Stack gap={2}>
              <Text fw="bold" size="sm" className={classes.mono}>
                {item?.score}
              </Text>
              <Text size="xs">{t('game.label.score_table.score')}</Text>
            </Stack>
            <Stack gap={2}>
              <Text fw="bold" size="sm" className={classes.mono}>
                {item?.solvedCount}
              </Text>
              <Text size="xs">{t('game.label.score_table.solved_count')}</Text>
            </Stack>
          </Group>
          <Progress value={solved * 100} />
        </Stack>
        {item?.solvedCount && item?.solvedCount > 0 ? (
          <ScrollArea scrollbarSize={6} h="12rem" w="100%">
            <Table className={classes.table}>
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
                {item?.challenges &&
                  challengeIdMap &&
                  item.challenges
                    .filter((c) => c.type !== SubmissionType.Unaccepted)
                    .sort((a, b) => dayjs(b.time).diff(dayjs(a.time)))
                    .map((chal) => {
                      const info = challengeIdMap.get(chal.id!)
                      return (
                        <Table.Tr key={chal.id}>
                          <Table.Td style={{ fontWeight: 500 }}>{chal.userName}</Table.Td>
                          <Table.Td>{info?.title}</Table.Td>
                          <Table.Td className={classes.mono}>{info?.tag}</Table.Td>
                          <Table.Td className={classes.mono}>
                            {chal.score}
                            {chal.score! > info?.score! &&
                              chal.type &&
                              BloodsTypes.includes(chal.type) && (
                                <Text span c="dimmed" className={classes.mono}>
                                  {` (${bloodBonusMap.get(chal.type)?.descr})`}
                                </Text>
                              )}
                          </Table.Td>
                          <Table.Td className={classes.mono}>
                            {dayjs(chal.time).format('MM/DD HH:mm:ss')}
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

export default ScoreboardItemModal
