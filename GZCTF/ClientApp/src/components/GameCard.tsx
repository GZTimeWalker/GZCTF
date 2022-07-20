import { Card, Group, Image, Stack, Text, Title, useMantineTheme } from "@mantine/core";
import { FC } from "react";
import Icon from '@mdi/react';
import { mdiFlag, mdiFlagOutline, mdiGift, mdiGiftOutline, mdiMinusCircle, mdiPackageVariantClosed, mdiSleep } from "@mdi/js";

interface GameCardProps {
  state: number;
  cover: string;
};

const GameCard: FC<GameCardProps> = (props) => {
  const { state, cover } = props;
  const theme = useMantineTheme();

  return (
    <Card
      shadow="sm"
      sx={(theme) => ({
        cursor: 'pointer',
        transition: 'filter .2s',
        '&:hover': {
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
        },
      })}
    >
      <Card.Section>
        {cover ?
          <Image src={cover} height={160} alt="cover" />
          :
          <div style={{ height: 160, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </div>
        }
      </Card.Section>
      <Stack style={{ flexGrow: 1 }}>
        <Group align="end" position="apart">
          <Title order={2} align="left">
            CTF Game
          </Title>
          <Text size="md">2022 / 5 / 26 ~ 2022 / 7 / 26</Text>
        </Group>
        <Text size="md" lineClamp={1}>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</Text>
      </Stack>
    </Card>
  )
}

export default GameCard;