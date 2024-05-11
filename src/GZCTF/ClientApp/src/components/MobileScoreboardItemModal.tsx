import {
  Avatar,
  Badge,
  Center,
  Group,
  Input,
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
import cx from 'clsx'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import TeamRadarMap from '@Components/TeamRadarMap'
import { BonusLabel } from '@Utils/Shared'
import { ChallengeInfo, ScoreboardItem, SubmissionType } from '@Api'
import tableClasses from '@Styles/Table.module.css'

interface MobileScoreboardItemModalProps extends ModalProps {
  item?: ScoreboardItem | null
  bloodBonusMap: Map<SubmissionType, BonusLabel>
  challenges?: Record<string, ChallengeInfo[]>
}

const MobileScoreboardItemModal: FC<MobileScoreboardItemModalProps> = (props) => {
  const { item, challenges, ...modalProps } = props
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
          <Avatar alt="avatar" src={item?.avatar} size={50} radius="md">
            {item?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Stack gap={0}>
            <Group gap={4}>
              <Title order={4} lineClamp={1}>
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
            <Stack gap={1}>
              <Text fw="bold" size="sm" ff="monospace">
                {item?.rank}
              </Text>
              <Text size="xs">{t('game.label.score_table.rank_total')}</Text>
            </Stack>
            {item?.organization && (
              <Stack gap={1}>
                <Text fw="bold" size="sm" ff="monospace">
                  {item?.organizationRank}
                </Text>
                <Text size="xs">{t('game.label.score_table.rank_organization')}</Text>
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
            <Table className={cx(tableClasses.table, tableClasses.nopadding)} fz="sm">
              <Table.Thead>
                <Table.Tr>
                  <Table.Th style={{ minWidth: '3rem' }}>{t('common.label.challenge')}</Table.Th>
                  <Table.Th style={{ minWidth: '3rem' }}>
                    {t('game.label.score_table.score')}
                  </Table.Th>
                  <Table.Th style={{ minWidth: '3rem' }}>{t('common.label.time')}</Table.Th>
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
                          <Table.Td>
                            <Input
                              variant="unstyled"
                              value={info?.title}
                              readOnly
                              size="sm"
                              sx={{
                                wrapper: {
                                  width: '100%',
                                },

                                input: {
                                  userSelect: 'none',
                                  fontSize: '0.85rem',
                                  lineHeight: '0.9rem',
                                  height: '0.9rem',
                                  fontWeight: 500,

                                  '&:hover': {
                                    textDecoration: 'underline',
                                  },
                                },
                              }}
                            />
                          </Table.Td>
                          <Table.Td ff="monospace">{chal.score}</Table.Td>
                          <Table.Td ff="monospace">
                            {dayjs(chal.time).format('MM/DD HH:mm')}
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

export default MobileScoreboardItemModal
